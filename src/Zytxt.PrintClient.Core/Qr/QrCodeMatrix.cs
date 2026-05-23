namespace Zytxt.PrintClient.Core.Qr;

public sealed class QrCodeMatrix
{
    private readonly bool[,] modules;

    public QrCodeMatrix(bool[,] modules)
    {
        this.modules = modules;
        Size = modules.GetLength(0);
        DarkModuleCount = CountDarkModules(modules);
    }

    public int Size { get; }

    public int DarkModuleCount { get; }

    public bool HasDarkModule(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Size || y >= Size)
        {
            return false;
        }

        return modules[y, x];
    }

    private static int CountDarkModules(bool[,] modules)
    {
        var count = 0;
        for (var y = 0; y < modules.GetLength(0); y++)
        {
            for (var x = 0; x < modules.GetLength(1); x++)
            {
                if (modules[y, x])
                {
                    count++;
                }
            }
        }

        return count;
    }
}
