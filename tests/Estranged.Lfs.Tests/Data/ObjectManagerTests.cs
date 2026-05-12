using Estranged.Lfs.Data;
using Estranged.Lfs.Data.Entities;
using Moq;
using System.Collections.Generic;
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
    }
}
