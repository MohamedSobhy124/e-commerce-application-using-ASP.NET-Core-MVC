
using BulkyBook.Models;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace BulkyBook.Utility
{
    public static class GuestCartHelper
    {
        private const string CartSessionKey = "GuestCart";

        public static List<GuestCartItem> GetGuestCart(ISession session)
        {
            var cartJson = session.GetString(CartSessionKey);
            if (string.IsNullOrEmpty(cartJson))
            {
                return new List<GuestCartItem>();
            }

            return JsonSerializer.Deserialize<List<GuestCartItem>>(cartJson) ?? new List<GuestCartItem>();
        }

        public static void SaveGuestCart(ISession session, List<GuestCartItem> cart)
        {
            var cartJson = JsonSerializer.Serialize(cart);
            session.SetString(CartSessionKey, cartJson);
        }

        public static void AddToCart(ISession session, int productId, int count = 1, int? productVariantId = null, int? comboOfferId = null, int? flashSaleItemId = null, double? flashSalePrice = null)
        {
            var cart = GetGuestCart(session);
            
            // For combo offers, check if combo already exists (only check ComboOfferId)
            if (comboOfferId.HasValue)
            {
                var existingComboItem = cart.FirstOrDefault(c => c.ComboOfferId == comboOfferId);
                if (existingComboItem != null)
                {
                    existingComboItem.Count += count;
                }
                else
                {
                    cart.Add(new GuestCartItem
                    {
                        ProductId = productId,
                        Count = count,
                        ProductVariantId = productVariantId,
                        ComboOfferId = comboOfferId
                    });
                }
            }
            else
            {
                // Check for existing item with same product, variant, and flash sale status
                var existingItem = cart.FirstOrDefault(c => c.ProductId == productId 
                    && c.ProductVariantId == productVariantId 
                    && c.FlashSaleItemId == flashSaleItemId
                    && !c.ComboOfferId.HasValue);

                if (existingItem != null)
                {
                    existingItem.Count += count;
                }
                else
                {
                    cart.Add(new GuestCartItem
                    {
                        ProductId = productId,
                        Count = count,
                        ProductVariantId = productVariantId,
                        FlashSaleItemId = flashSaleItemId,
                        FlashSalePrice = flashSalePrice
                    });
                }
            }

            SaveGuestCart(session, cart);
        }

        public static void RemoveFromCart(ISession session, int productId)
        {
            var cart = GetGuestCart(session);
            cart.RemoveAll(c => c.ProductId == productId);
            SaveGuestCart(session, cart);
        }

        public static void UpdateQuantity(ISession session, int productId, int count)
        {
            var cart = GetGuestCart(session);
            var item = cart.FirstOrDefault(c => c.ProductId == productId);

            if (item != null)
            {
                if (count <= 0)
                {
                    cart.Remove(item);
                }
                else
                {
                    item.Count = count;
                }
                SaveGuestCart(session, cart);
            }
        }

        public static void ClearCart(ISession session)
        {
            session.Remove(CartSessionKey);
        }

        public static int GetCartCount(ISession session)
        {
            return GetGuestCart(session).Count;
        }
    }
}

