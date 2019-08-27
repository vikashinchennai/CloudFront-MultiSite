namespace Sitecore.Foundation.CloudFront.Extensions
{
    using Sitecore;
    using Sitecore.Data;
    using Sitecore.Data.Fields;
    using Sitecore.Data.Items;
    using Sitecore.Resources.Media;
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Web;
    public static class FieldExtensions
    {
       
        public static string ImageUrl(this ImageField imageField)
        {
            if (imageField?.MediaItem == null)
            {
                throw new ArgumentNullException(nameof(imageField));
            }

            var options = MediaUrlOptions.Empty;
            int width, height;

            if (int.TryParse(imageField.Width, out width))
            {
                options.Width = width;
            }

            if (int.TryParse(imageField.Height, out height))
            {
                options.Height = height;
            }
            return imageField.ImageUrl(options);
        }

        public static string ImageUrl(this ImageField imageField, MediaUrlOptions options)
        {
            if (imageField?.MediaItem == null)
            {
                throw new ArgumentNullException(nameof(imageField));
            }

            return options == null ? imageField.ImageUrl() : HashingUtils.ProtectAssetUrl(MediaManager.GetMediaUrl(imageField.MediaItem, options));
        }

        public static bool IsChecked(this Field checkboxField)
        {
            if (checkboxField == null)
            {
                throw new ArgumentNullException(nameof(checkboxField));
            }
            return MainUtil.GetBool(checkboxField?.Value, false);
        }

        public static bool IsChecked(this Item item, string checkBoxFieldName)
        {
            return IsFieldExists(item, checkBoxFieldName) && IsChecked(item.Fields[checkBoxFieldName]);
        }

        public static Item[] GetMultiListItemsFromField(this Item item, string fieldName)
        {
            if (item.IsFieldExists(fieldName))
            {
                MultilistField multilistField = item.Fields[fieldName];
                return multilistField.GetItems();
            }
            return new Item[] { };
        }

        public static bool IsFieldExists(this Item item, string fieldName)
        {
            return (item != null && item.Fields != null && item.Fields[fieldName] != null);
        }

        public static bool HasValue(this Item item, string fieldName)
        {
            return IsFieldExists(item, fieldName) && !string.IsNullOrEmpty(item.Fields[fieldName].Value);
        }

        public static string GetFieldValue(this Item item, string fieldName, string defaultValue = "")
        {
            return (item.IsFieldExists(fieldName) && item.HasValue(fieldName))
                    ? item.Fields[fieldName].Value
                    : defaultValue;
        }

        public static string GetFieldValue(this Item item, ID fieldId, string defaultValue = "")
        {
            return item.HasValue(fieldId)
                    ? item.Fields[fieldId].Value
                    : defaultValue;
        }
        public static bool IsFieldExists(this Item item, ID fieldId)
        {
            return (item != null && item.Fields != null && item.Fields[fieldId] != null);
        }

        public static bool HasValue(this Item item, ID fieldId)
        {
            return IsFieldExists(item, fieldId) && !string.IsNullOrEmpty(item.Fields[fieldId].Value);
        }

        public static Guid GetLinkFieldId(this Item item, string fieldName)
        {
            if (item.IsFieldExists(fieldName))
            {
                LinkField field = item.Fields[fieldName];
                return field.TargetID.ToGuid();
            }
            return Guid.Empty;
        }

        public static Dictionary<string, object> ExtractMapperInfo(this NameValueCollection QueryStringMapping, Dictionary<string, object> inputData)
        {
            Dictionary<string, object> output = new Dictionary<string, object>();
            if (QueryStringMapping == null)
                return output;

            foreach (var eachKey in QueryStringMapping.AllKeys)
            {
                var value = inputData == null ? string.Empty : inputData?[eachKey] ?? string.Empty;
                if (!string.IsNullOrEmpty(QueryStringMapping[eachKey]) && !output.ContainsKey(QueryStringMapping[eachKey]))
                    output.Add(QueryStringMapping[eachKey],value);
            }
            return output;

        }

        public static Dictionary<string, object> ExtractMapperInfoQuerystring(this NameValueCollection QueryStringMapping, Dictionary<string, object> inputData)
        {
            Dictionary<string, object> output = new Dictionary<string, object>();
            if (QueryStringMapping == null)
                return output;

            foreach (var eachKey in QueryStringMapping.AllKeys)
            {
                var value = inputData == null ? string.Empty : inputData?[eachKey] ?? string.Empty;
                if (!string.IsNullOrEmpty(value.ToString()) && !output.ContainsKey(value.ToString()))
                    output.Add(value.ToString(),QueryStringMapping[eachKey]);
            }
            return output;

        }

        public static string RelaceTokenWithQueryStringValue(this string inputString)
        {
            if (string.IsNullOrEmpty(inputString))
                return string.Empty;
            var getAllQueryString = HttpContext.Current?.Request?.QueryString;
            return RelaceTokenWithQueryStringValue(inputString, getAllQueryString);
        }

        public static string RelaceTokenWithQueryStringValue(this string inputString, NameValueCollection filterCollection)
        {
                     
            if (string.IsNullOrEmpty(inputString))
                return null;
            var getAllQueryString = HttpContext.Current?.Request?.QueryString;

            var fieldCollection = ConvertIntoDictonary(filterCollection);
            var data = ExtractMapperInfoQuerystring(getAllQueryString, fieldCollection);

            return RelaceTokenWithQueryStringValue(inputString, data);
        }

        private static string RelaceTokenWithQueryStringValue(this string inputString, Dictionary<string, object> replaceCollection, string token = "$")
        {
            if (string.IsNullOrEmpty(inputString))
                return string.Empty;

            if (replaceCollection == null || replaceCollection.Count == 0)
                return inputString;
            foreach (var key in replaceCollection.Keys)
            {
                inputString = inputString.Replace(token + key + token, System.Convert.ToString(replaceCollection[key] ?? string.Empty));
            }
            return inputString;
        }
        private static string RelaceTokenWithQueryStringValue(this string inputString, NameValueCollection replaceCollection, string token= "$")
        {
            if (string.IsNullOrEmpty(inputString))
                return string.Empty;
           
            if (replaceCollection == null || replaceCollection.Count == 0)
                return inputString;
            foreach (var item in replaceCollection.AllKeys)
            {
                inputString = inputString.Replace(token + item + token, replaceCollection[item]);

            }
            return inputString;
        }

        private static Dictionary<string, object> ConvertIntoDictonary(this NameValueCollection input)
        {
            Dictionary<string, object> output = new Dictionary<string, object>();
            if (input == null)
                return output;

            foreach (var eachKey in input.AllKeys)
            {
                output.Add(eachKey, input[eachKey]);
            }
            return output;
        }

       
    }
}