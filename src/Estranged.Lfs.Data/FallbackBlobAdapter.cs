using System.Threading;
using System.Threading.Tasks;

namespace Estranged.Lfs.Data
{
    public sealed class FallbackBlobAdapter : IBlobAdapter
    {
        private readonly IBlobAdapter primary;
        private readonly IBlobAdapter fallback;

        public FallbackBlobAdapter(IBlobAdapter primary, IBlobAdapter fallback)
        {
            this.primary = primary;
            this.fallback = fallback;
        }

        public Task<SignedBlob> UriForUpload(string oid, long size, CancellationToken token)
        {
            return primary.UriForUpload(oid, size, token);
        }

        public async Task<SignedBlob> UriForDownload(string oid, CancellationToken token)
        {
            var signedBlob = await primary.UriForDownload(oid, token).ConfigureAwait(false);
            if (signedBlob.ErrorCode == 404)
            {
                return await fallback.UriForDownload(oid, token).ConfigureAwait(false);
            }

            return signedBlob;
        }
    }
}
