using Microsoft.AspNetCore.Mvc.ModelBinding;
using IdealWeightNutrition.Utility;

namespace IdealWeightNutrition.ModelBinders
{
    /// <summary>
    /// Model binder that automatically decrypts encrypted IDs from URLs
    /// </summary>
    public class EncryptedIdModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            var modelName = bindingContext.ModelName;
            var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);

            if (valueProviderResult == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);

            var value = valueProviderResult.FirstValue;

            if (string.IsNullOrEmpty(value))
            {
                return Task.CompletedTask;
            }

            // Try to decrypt the ID
            int? decryptedId = IdEncryptionHelper.DecryptId(value);

            if (decryptedId.HasValue)
            {
                bindingContext.Result = ModelBindingResult.Success(decryptedId.Value);
            }
            else
            {
                // If decryption fails, try parsing as regular int (for backward compatibility)
                if (int.TryParse(value, out int regularId))
                {
                    bindingContext.Result = ModelBindingResult.Success(regularId);
                }
                else
                {
                    bindingContext.ModelState.TryAddModelError(modelName, $"Invalid encrypted ID format: {value}");
                }
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Provider for encrypted ID model binder
    /// </summary>
    public class EncryptedIdModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // Apply to int parameters named 'id', 'orderId', 'productId', etc.
            var modelName = context.Metadata.Name?.ToLower();
            if (context.Metadata.ModelType == typeof(int) || context.Metadata.ModelType == typeof(int?))
            {
                if (modelName == "id" || 
                    modelName == "orderid" || 
                    modelName == "productid" || 
                    modelName == "categoryid" || 
                    modelName == "brandid" ||
                    modelName == "flashsaleid" ||
                    modelName == "comboid" ||
                    modelName == "serviceid" ||
                    modelName == "promocodeid" ||
                    modelName?.EndsWith("id") == true)
                {
                    return new EncryptedIdModelBinder();
                }
            }

            return null;
        }
    }
}

