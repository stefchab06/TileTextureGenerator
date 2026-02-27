using TileTextureGenerator.Core.Enums;
using TileTextureGenerator.Core.Services;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TileTextureGenerator.Core.Entities
{
    public abstract class TileTextureProjectBase
    {
        public string Name { get; }
        public String Type { get; set; } = string.Empty;
        private ProjectStatus _Status = ProjectStatus.Unexisting;

        public ProjectStatus Status
        {
            get => _Status;
            set => _Status = value;
        }

        public byte[]? DisplayImage { get; set; }

        /// <summary>
        /// Last modification date (UTC)
        /// </summary>
        public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;

        protected TileTextureProjectBase(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Sets the display image from raw image data (not file path)
        /// Converts to PNG 256x256 for display purposes
        /// </summary>
        protected void SetDisplayImageFromImageData(byte[] imageData, IImageProcessingService imageProcessor)
        {
            if (imageData == null || imageData.Length == 0)
                throw new ArgumentException("Image data cannot be null or empty", nameof(imageData));

            if (imageProcessor == null)
                throw new ArgumentNullException(nameof(imageProcessor));

            // Convert to PNG 256x256 for DisplayImage
            DisplayImage = imageProcessor.ConvertToPng(imageData, 256, 256);
        }

        /// <summary>
        /// Serializes the project to JSON. Override in derived classes to add custom properties.
        /// Note: DisplayImage is NOT serialized here - it's handled by the persistence layer
        /// </summary>
        public virtual string ToJson()
        {
            var jsonObject = new JsonObject
            {
                ["Type"] = Type,
                ["Name"] = Name,
                ["Status"] = Status.ToString(),
                ["LastModifiedDate"] = LastModifiedDate.ToString("O")  // ISO 8601 format
            };

            AddCustomPropertiesToJson(jsonObject);

            return jsonObject.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        }

        /// <summary>
        /// Loads project data from JSON. Override in derived classes to load custom properties.
        /// Note: DisplayImage is NOT loaded here - it's handled by the persistence layer
        /// </summary>
        public virtual void LoadFromJson(string jsonContent)
        {
            var jsonDoc = JsonDocument.Parse(jsonContent);
            var root = jsonDoc.RootElement;

            // Load Status if present
            if (root.TryGetProperty("Status", out var statusElement))
            {
                if (Enum.TryParse<ProjectStatus>(statusElement.GetString(), out var status))
                {
                    Status = status;
                }
            }

            // Load Type if present
            if (root.TryGetProperty("Type", out var typeElement))
            {
                var typeValue = typeElement.GetString();
                if (!string.IsNullOrEmpty(typeValue))
                {
                    Type = typeValue;
                }
            }

            // Load LastModifiedDate if present, otherwise use current time
            if (root.TryGetProperty("LastModifiedDate", out var dateElement))
            {
                if (DateTime.TryParse(dateElement.GetString(), out var date))
                {
                    LastModifiedDate = date;
                }
            }
            else
            {
                LastModifiedDate = DateTime.UtcNow;
            }

            // Allow derived classes to load their custom properties
            LoadCustomPropertiesFromJson(root);
        }

        /// <summary>
        /// Override this method in derived classes to add custom properties to the JSON.
        /// </summary>
        protected virtual void AddCustomPropertiesToJson(JsonObject jsonObject)
        {
            // Base implementation does nothing
        }

        /// <summary>
        /// Override this method in derived classes to load custom properties from JSON.
        /// </summary>
        protected virtual void LoadCustomPropertiesFromJson(JsonElement rootElement)
        {
            // Base implementation does nothing
        }
    }
}
