using Microsoft.Extensions.Caching.Memory;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace IdealWeightNutrition.Utility
{
    /// <summary>
    /// Helper class for generating and managing OTP (One-Time Password) for email verification
    /// </summary>
    public class OtpHelper
    {
        private readonly IMemoryCache _memoryCache;
        private const int OTP_LENGTH = 6;
        private const int OTP_EXPIRY_MINUTES = 10;
        private const int MAX_ATTEMPTS = 5;

        public OtpHelper(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// Generates a secure 6-digit OTP
        /// </summary>
        public string GenerateOtp()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var bytes = new byte[4];
                rng.GetBytes(bytes);
                var random = BitConverter.ToUInt32(bytes, 0);
                // Generate 6-digit OTP (100000 to 999999)
                var otp = (random % 900000) + 100000;
                return otp.ToString("D6");
            }
        }

        /// <summary>
        /// Stores OTP in memory cache with expiration
        /// </summary>
        public void StoreOtp(string email, string otp)
        {
            var cacheKey = GetOtpCacheKey(email);
            var otpData = new OtpData
            {
                Otp = otp,
                Email = email,
                CreatedAt = DateTime.UtcNow,
                Attempts = 0
            };

            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(OTP_EXPIRY_MINUTES),
                SlidingExpiration = TimeSpan.FromMinutes(OTP_EXPIRY_MINUTES)
            };

            _memoryCache.Set(cacheKey, otpData, cacheOptions);
        }

        /// <summary>
        /// Verifies the OTP for the given email
        /// </summary>
        public OtpVerificationResult VerifyOtp(string email, string otp)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(otp))
            {
                return new OtpVerificationResult
                {
                    IsValid = false,
                    Message = "Email and OTP are required"
                };
            }

            var cacheKey = GetOtpCacheKey(email);
            if (!_memoryCache.TryGetValue(cacheKey, out OtpData otpData))
            {
                return new OtpVerificationResult
                {
                    IsValid = false,
                    Message = "OTP has expired or not found. Please request a new OTP."
                };
            }

            // Check if too many attempts
            if (otpData.Attempts >= MAX_ATTEMPTS)
            {
                _memoryCache.Remove(cacheKey);
                return new OtpVerificationResult
                {
                    IsValid = false,
                    Message = "Too many failed attempts. Please request a new OTP."
                };
            }

            // Increment attempts
            otpData.Attempts++;
            _memoryCache.Set(cacheKey, otpData);

            // Verify OTP
            if (otpData.Otp.Equals(otp, StringComparison.OrdinalIgnoreCase))
            {
                // Mark as verified
                var verifiedKey = GetVerifiedCacheKey(email);
                _memoryCache.Set(verifiedKey, true, TimeSpan.FromMinutes(30)); // Verified status valid for 30 minutes
                
                // Remove OTP from cache after successful verification
                _memoryCache.Remove(cacheKey);
                
                return new OtpVerificationResult
                {
                    IsValid = true,
                    Message = "Email verified successfully"
                };
            }

            return new OtpVerificationResult
            {
                IsValid = false,
                Message = $"Invalid OTP. {MAX_ATTEMPTS - otpData.Attempts} attempts remaining."
            };
        }

        /// <summary>
        /// Checks if email is verified
        /// </summary>
        public bool IsEmailVerified(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            var verifiedKey = GetVerifiedCacheKey(email);
            return _memoryCache.TryGetValue(verifiedKey, out bool verified) && verified;
        }

        /// <summary>
        /// Clears verification status for an email (useful for testing or re-verification)
        /// </summary>
        public void ClearVerification(string email)
        {
            var verifiedKey = GetVerifiedCacheKey(email);
            _memoryCache.Remove(verifiedKey);
        }

        /// <summary>
        /// Gets remaining time for OTP expiration
        /// </summary>
        public int? GetOtpRemainingMinutes(string email)
        {
            var cacheKey = GetOtpCacheKey(email);
            if (_memoryCache.TryGetValue(cacheKey, out OtpData otpData))
            {
                var elapsed = DateTime.UtcNow - otpData.CreatedAt;
                var remaining = OTP_EXPIRY_MINUTES - (int)elapsed.TotalMinutes;
                return remaining > 0 ? remaining : 0;
            }
            return null;
        }

        private string GetOtpCacheKey(string email)
        {
            return $"OTP_{email.ToLowerInvariant()}";
        }

        private string GetVerifiedCacheKey(string email)
        {
            return $"VERIFIED_{email.ToLowerInvariant()}";
        }

        private class OtpData
        {
            public string Otp { get; set; }
            public string Email { get; set; }
            public DateTime CreatedAt { get; set; }
            public int Attempts { get; set; }
        }
    }

    public class OtpVerificationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
    }
}
