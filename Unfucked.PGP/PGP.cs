using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;
using PgpCore.Abstractions;
using System.Text;

// ReSharper disable InconsistentNaming - named after types in third-party library

namespace Unfucked;

/// <summary>
/// Like <see cref="PgpCore.PGP"/>, but with the added ability to generate detached signatures (<c>gpg --sign --detach-sign --armor</c>)
/// </summary>
public interface IPGP: PgpCore.Abstractions.IPGP {

    /// <summary>
    /// <c>gpg --sign --detach-sign --armor</c>
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    Task DetachedSignAsync(Stream inputStream, Stream outputStream, IDictionary<string, string>? headers = null);

    /// <summary>
    /// <c>gpg --sign --detach-sign --armor</c>
    /// </summary>
    Task<string> DetachedSignAsync(string input, IDictionary<string, string>? headers = null);

}

/// <inheritdoc cref="IPGP"/>
public class PGP: PgpCore.PGP, IPGP {

    public PGP() { }
    public PGP(IEncryptionKeys encryptionKeys): base(encryptionKeys) { }

    // Copied from PGP.ClearSignAsync(Stream,Stream,IDictionary<string,string>) and PGP.SignAsync(Stream,Stream,bool,string,IDictionary<string,string>,bool)
    /// <inheritdoc />
    public async Task DetachedSignAsync(Stream inputStream, Stream outputStream, IDictionary<string, string>? headers = null) {
        if (EncryptionKeys == null) {
            throw new ArgumentException("EncryptionKeys");
        } else if (inputStream.Position != 0) {
            throw new ArgumentException("inputStream should be at start of stream");
        }

        headers ??= new Dictionary<string, string>(0);

        using ArmoredOutputStream armoredOutputStream   = new(outputStream, headers);
        PgpSignatureGenerator     pgpSignatureGenerator = InitDetachedSignatureGenerator();
        int                       length;
        byte[]                    buf = new byte[65535];
        while ((length = await inputStream.ReadAsync(buf, 0, buf.Length).ConfigureAwait(false)) > 0) {
            pgpSignatureGenerator.Update(buf, 0, length);
        }

        using BcpgOutputStream bcpgOutputStream = new(armoredOutputStream);
        pgpSignatureGenerator.Generate().Encode(bcpgOutputStream);
    }

    // Copied from PGP.ClearSignAsync(string,IDictionary<string,string>)
    /// <inheritdoc />
    public async Task<string> DetachedSignAsync(string input, IDictionary<string, string>? headers = null) {
        headers ??= new Dictionary<string, string>(0);

        using Stream inputStream  = input.ToStream();
        using Stream outputStream = new MemoryStream();
        await DetachedSignAsync(inputStream, outputStream, headers).ConfigureAwait(false);
        outputStream.Seek(0, SeekOrigin.Begin);
        using StreamReader outputStreamReader = new(outputStream, Encoding.UTF8);
        return await outputStreamReader.ReadToEndAsync().ConfigureAwait(false);
    }

    // Copied from PGPCore.InitClearSignatureGenerator(ArmoredOutputStream)
    private PgpSignatureGenerator InitDetachedSignatureGenerator() {
        PublicKeyAlgorithmTag tag                   = EncryptionKeys.SigningSecretKey.PublicKey.Algorithm;
        PgpSignatureGenerator pgpSignatureGenerator = new(tag, HashAlgorithmTag);
        pgpSignatureGenerator.InitSign(PgpSignature.CanonicalTextDocument, EncryptionKeys.SigningPrivateKey);
        foreach (string userId in EncryptionKeys.SigningSecretKey.PublicKey.GetUserIds()) {
            PgpSignatureSubpacketGenerator subPacketGenerator = new();
            subPacketGenerator.AddSignerUserId(false, userId);
            pgpSignatureGenerator.SetHashedSubpackets(subPacketGenerator.Generate());
            // Just the first one!
            break;
        }

        return pgpSignatureGenerator;
    }

}