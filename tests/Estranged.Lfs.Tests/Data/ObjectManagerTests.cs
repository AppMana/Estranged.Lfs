using Estranged.Lfs.Data;
using Estranged.Lfs.Data.Entities;
using Moq;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Estranged.Lfs.Tests.Data
{
    public class ObjectManagerTests
    {
        [Fact]
        public async Task DownloadObjectsReturnsBlobAdapterErrorForMissingObject()
        {
            var blobAdapter = new Mock<IBlobAdapter>(MockBehavior.Strict);
            blobAdapter
                .Setup(x => x.UriForDownload("missing-oid", CancellationToken.None))
                .ReturnsAsync(new SignedBlob
                {
                    ErrorCode = 404,
                    ErrorMessage = "Object not found"
                });

            var manager = new ObjectManager(blobAdapter.Object);

            var response = (await manager.DownloadObjects(new List<RequestObject>
            {
                new RequestObject { Oid = "missing-oid", Size = 123 }
            }, CancellationToken.None)).Single();

            Assert.Equal("missing-oid", response.Oid);
            Assert.Equal(123, response.Size);
            Assert.Null(response.Authenticated);
            Assert.NotNull(response.Error);
            Assert.Equal(404, response.Error.Code);
            Assert.Equal("Object not found", response.Error.Message);
            Assert.Null(response.Actions.Download);
            blobAdapter.VerifyAll();
        }

        [Fact]
        public async Task FallbackBlobAdapterUsesFallbackOnlyForMissingDownloads()
        {
            var primary = new Mock<IBlobAdapter>(MockBehavior.Strict);
            var fallback = new Mock<IBlobAdapter>(MockBehavior.Strict);

            primary.Setup(x => x.UriForDownload("missing-oid", CancellationToken.None))
                   .ReturnsAsync(new SignedBlob { ErrorCode = 404, ErrorMessage = "Object not found" });
            fallback.Setup(x => x.UriForDownload("missing-oid", CancellationToken.None))
                    .ReturnsAsync(new SignedBlob { Uri = new Uri("https://fallback.example/missing-oid"), Size = 123 });
            primary.Setup(x => x.UriForDownload("forbidden-oid", CancellationToken.None))
                   .ReturnsAsync(new SignedBlob { ErrorCode = 403, ErrorMessage = "Forbidden" });
            primary.Setup(x => x.UriForUpload("upload-oid", 456, CancellationToken.None))
                   .ReturnsAsync(new SignedBlob { Uri = new Uri("https://primary.example/upload-oid") });

            var adapter = new FallbackBlobAdapter(primary.Object, fallback.Object);

            var missing = await adapter.UriForDownload("missing-oid", CancellationToken.None);
            var forbidden = await adapter.UriForDownload("forbidden-oid", CancellationToken.None);
            var upload = await adapter.UriForUpload("upload-oid", 456, CancellationToken.None);

            Assert.Equal(new Uri("https://fallback.example/missing-oid"), missing.Uri);
            Assert.Equal(123, missing.Size);
            Assert.Equal(403, forbidden.ErrorCode);
            Assert.Equal(new Uri("https://primary.example/upload-oid"), upload.Uri);
            primary.VerifyAll();
            fallback.VerifyAll();
        }
    }
}
