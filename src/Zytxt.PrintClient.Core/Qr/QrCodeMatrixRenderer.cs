using QRCoder;

namespace Zytxt.PrintClient.Core.Qr;

public sealed class QrCodeMatrixRenderer
{
    public QrCodeMatrix Render(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new ArgumentException("QR payload cannot be empty.", nameof(payload));
        }

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload.Trim(), QRCodeGenerator.ECCLevel.Q);
        var size = data.ModuleMatrix.Count;
        var modules = new bool[size, size];

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                modules[y, x] = data.ModuleMatrix[y][x];
            }
        }

        return new QrCodeMatrix(modules);
    }
}
