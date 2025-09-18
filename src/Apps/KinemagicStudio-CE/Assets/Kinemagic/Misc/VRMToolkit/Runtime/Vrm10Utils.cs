using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UniGLTF;
using UnityEngine;
using UniVRM10;

namespace VRMToolkit
{
    public static class Vrm10Utils
    {
        public static async Task<VrmMetadata> LoadVrmMetadataAsync(
            string path,
            int thumbnailWidth,
            int thumbnailHeight,
            bool canLoadVrm0X = true,
            IAwaitCaller awaitCaller = null,
            ITextureDeserializer textureDeserializer = null,
            CancellationToken cancellationToken = default)
        {
            awaitCaller ??= UnityEngine.Application.isPlaying
                ? new RuntimeOnlyAwaitCaller()
                : new ImmediateCaller();

            using var gltfData = await awaitCaller.Run(() =>
            {
                var bytes = File.ReadAllBytes(path);
                return new GlbLowLevelParser(path, bytes).Parse();
            });

            return await LoadVrmMetadataAsync(
                gltfData,
                thumbnailWidth,
                thumbnailHeight,
                canLoadVrm0X,
                awaitCaller,
                textureDeserializer,
                cancellationToken);
        }

        public static async Task<VrmMetadata> LoadVrmMetadataAsync(
            byte[] bytes,
            int thumbnailWidth,
            int thumbnailHeight,
            bool canLoadVrm0X = true,
            IAwaitCaller awaitCaller = null,
            ITextureDeserializer textureDeserializer = null,
            CancellationToken cancellationToken = default)
        {
            awaitCaller ??= UnityEngine.Application.isPlaying
                ? new RuntimeOnlyAwaitCaller()
                : new ImmediateCaller();

            using var gltfData = await awaitCaller.Run(() => new GlbLowLevelParser(string.Empty, bytes).Parse());

            return await LoadVrmMetadataAsync(
                gltfData,
                thumbnailWidth,
                thumbnailHeight,
                canLoadVrm0X,
                awaitCaller,
                textureDeserializer,
                cancellationToken);
        }

        public static async Task<VrmMetadata> LoadVrmMetadataAsync(
            GltfData gltfData,
            int thumbnailWidth,
            int thumbnailHeight,
            bool canLoadVrm0X = true,
            IAwaitCaller awaitCaller = null,
            ITextureDeserializer textureDeserializer = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            awaitCaller ??= UnityEngine.Application.isPlaying
                ? new RuntimeOnlyAwaitCaller()
                : new ImmediateCaller();

            // Loading as VRM-1.0
            var vrm10Data = await awaitCaller.Run(() => Vrm10Data.Parse(gltfData));
            cancellationToken.ThrowIfCancellationRequested();

            if (vrm10Data != null)
            {
                using (var loader = new Vrm10Importer(
                           vrm10Data,
                           textureDeserializer: textureDeserializer))
                {
                    // NOTE:
                    // The texture obtained from the LoadVrmThumbnailAsync method
                    // is destroyed when the Vrm10Importer instance is disposed.
                    // Create a cloned texture to avoid missing error.
                    var tempTexture = await loader.LoadVrmThumbnailAsync(awaitCaller);
                    var thumbnail = CreateResizedTexture(tempTexture, thumbnailWidth, thumbnailHeight);
                    return new VrmMetadata(thumbnail, vrm10Data.VrmExtension.Meta, null);
                }
            }

            if (!canLoadVrm0X)
            {
                throw new Exception($"Failed to load as VRM 1.0");
            }

            // Migration from VRM-0.x to VRM-1.0
            Vrm10Data migratedVrm10Data = default;
            MigrationData migrationData = default;
            using var migratedGltfData = await awaitCaller.Run(() => Vrm10Data.Migrate(gltfData, out migratedVrm10Data, out migrationData));
            cancellationToken.ThrowIfCancellationRequested();

            if (migratedVrm10Data == null)
            {
                throw new Exception(migrationData?.Message ?? "Failed to migrate.");
            }

            using (var migratedDataLoader = new Vrm10Importer(
                migratedVrm10Data,
                textureDeserializer: textureDeserializer))
            {
                // NOTE:
                // The texture obtained from the LoadVrmThumbnailAsync method
                // is destroyed when the Vrm10Importer instance is disposed.
                // Create a cloned texture to avoid missing error.
                var tempTexture = await migratedDataLoader.LoadVrmThumbnailAsync(awaitCaller);
                var thumbnail = CreateResizedTexture(tempTexture, thumbnailWidth, thumbnailHeight);
                return new VrmMetadata(thumbnail, null, migrationData.OriginalMetaBeforeMigration);
            }
        }

        public static Texture2D CreateResizedTexture(Texture2D source, int width, int height)
        {
            var resizedTexture = new Texture2D(width, height, source.format, false);

            var previousActiveRT = RenderTexture.active;
            var tempRT = RenderTexture.GetTemporary(width, height);
            Graphics.Blit(source, tempRT);

            // Reads pixels from the current render target and writes them to a texture.
            RenderTexture.active = tempRT;
            resizedTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            resizedTexture.Apply();

            RenderTexture.active = previousActiveRT;
            RenderTexture.ReleaseTemporary(tempRT);

            return resizedTexture;
        }
    }
}