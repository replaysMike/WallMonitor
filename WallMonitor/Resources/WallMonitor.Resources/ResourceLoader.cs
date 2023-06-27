using SixLabors.ImageSharp;
using System.Reflection;

namespace WallMonitor.Resources
{
    /// <summary>
    /// Resource loading library
    /// </summary>
    public static class ResourceLoader
    {
        /// <summary>
        /// Load an image from internal resources
        /// </summary>
        /// <param name="name"></param>
        /// <param name="resourceType"></param>
        /// <param name="resolution"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        public static async Task<Image> LoadImageAsync(string name, ResourceType resourceType, ImageResourceResolution resolution)
        {
            var stream = LoadStream(name, resourceType, resolution);
            var img = await Image.LoadAsync(stream);
            return img;
        }

        /// <summary>
        /// Load a stream from internal resources
        /// </summary>
        /// <param name="name"></param>
        /// <param name="resourceType"></param>
        /// <param name="resolution"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        public static Stream LoadStream(string name, ResourceType resourceType, ImageResourceResolution resolution)
        {
            const string resourcesFolder = "InternalResources";
            var currentAssembly = Assembly.GetExecutingAssembly();
            var resourceTypePath = GetResourcePath(resourceType);
            var resourceResolutionPath = GetResourcePath(resolution);
            var resourceName = $"{currentAssembly.GetName().Name}.{resourcesFolder}.{resourceTypePath}.{resourceResolutionPath}.{name}";
            try
            {
                var stream = currentAssembly.GetManifestResourceStream(resourceName);
                return stream;
            }
            catch (Exception)
            {
                throw new FileNotFoundException($"Could not load {resourceName}");
            }
        }

        /// <summary>
        /// Load a sound from internal resources
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Stream LoadSound(string name)
        {
            const string resourcesFolder = "InternalResources";
            try
            {
                var currentAssembly = Assembly.GetExecutingAssembly();
                var resourceName = $"{currentAssembly.GetName().Name}.{resourcesFolder}.Sounds.{name}";
                using var stm = currentAssembly.GetManifestResourceStream(resourceName) ?? throw new Exception($"Resource '{name}' not found!");
                var byteStream = new MemoryStream();
                stm.CopyTo(byteStream);
                return byteStream;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private static string GetResourcePath(ResourceType resourceType)
        {
            switch (resourceType)
            {
                case ResourceType.Background:
                    return "Backgrounds";
                case ResourceType.Sprite:
                    return "Sprites";
            }

            throw new NotImplementedException($"Unhandled resource type: {resourceType}");
        }

        private static string GetResourcePath(ImageResourceResolution resolution)
        {
            // names starting with a digit get underscores prefixed
            switch (resolution)
            {
                case ImageResourceResolution.HD:
                    return "_2k";
                case ImageResourceResolution.HD4K:
                    return "_4k";
            }

            throw new NotImplementedException($"Unhandled resolution type: {resolution}");
        }
    }
}