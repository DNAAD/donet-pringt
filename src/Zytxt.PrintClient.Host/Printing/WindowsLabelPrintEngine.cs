using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using Zytxt.PrintClient.Core.NativeDrawing;
using Zytxt.PrintClient.Core.Qr;

namespace Zytxt.PrintClient.Host.Printing;

public sealed class WindowsLabelPrintEngine
{
    private readonly QrCodeMatrixRenderer qrCodeRenderer = new();

    public void Print(NativeLabelDrawingPlan plan, string printerName)
    {
        Print(plan, printerName, LabelPrintMode.Direct80x30);
    }

    public void Print(NativeLabelDrawingPlan plan, string printerName, LabelPrintMode mode)
    {
        using var document = CreatePrintDocument(plan, printerName, mode);
        document.PrintPage += (_, args) =>
        {
            if (args.Graphics is null)
            {
                return;
            }

            DrawPlanForPrint(args.Graphics, args.PageSettings, plan, mode);
            args.HasMorePages = false;
        };

        document.Print();
    }

    public PrintDocument CreatePrintDocument(NativeLabelDrawingPlan plan, string printerName)
    {
        return CreatePrintDocument(plan, printerName, LabelPrintMode.Direct80x30);
    }

    public PrintDocument CreatePrintDocument(NativeLabelDrawingPlan plan, string printerName, LabelPrintMode mode)
    {
        var document = new PrintDocument();
        document.DocumentName = "ZYTXT 80x30 Label";
        if (mode == LabelPrintMode.Direct80x30 || mode == LabelPrintMode.Direct80x30Landscape)
        {
            document.DefaultPageSettings.PaperSize = new PaperSize(
                mode == LabelPrintMode.Direct80x30 ? "ZYTXT 80x30 Direct" : "ZYTXT 80x30 Landscape",
                plan.PaperSize.WidthHundredthsInch,
                plan.PaperSize.HeightHundredthsInch);
            document.DefaultPageSettings.Landscape = mode == LabelPrintMode.Direct80x30Landscape;
        }
        else
        {
            document.DefaultPageSettings.PaperSize = new PaperSize(
                mode == LabelPrintMode.FeedNoRotation ? "ZYTXT 30x80 Feed" : "ZYTXT 30x80 Feed Landscape",
                plan.PaperSize.HeightHundredthsInch,
                plan.PaperSize.WidthHundredthsInch);
            document.DefaultPageSettings.Landscape = true;
        }

        document.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
        document.OriginAtMargins = false;

        if (!string.IsNullOrWhiteSpace(printerName))
        {
            document.PrinterSettings.PrinterName = printerName;
        }

        return document;
    }

    public void DrawPlan(Graphics graphics, NativeLabelDrawingPlan plan)
    {
        PrepareGraphics(graphics);
        DrawCommands(graphics, plan);
    }

    private static void PrepareGraphics(Graphics graphics)
    {
        graphics.PageUnit = GraphicsUnit.Millimeter;
        graphics.SmoothingMode = SmoothingMode.None;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
    }

    private void DrawCommands(Graphics graphics, NativeLabelDrawingPlan plan)
    {
        foreach (var command in plan.Commands)
        {
            DrawCommand(graphics, command);
        }
    }

    public byte[] RenderPreviewPng(NativeLabelDrawingPlan plan, decimal dpi = 300m)
    {
        var converter = new PrintUnitConverter(dpi);
        var widthPixels = converter.MillimetersToPixels(plan.PaperSize.WidthMm);
        var heightPixels = converter.MillimetersToPixels(plan.PaperSize.HeightMm);

        using var bitmap = new Bitmap(widthPixels, heightPixels);
        bitmap.SetResolution((float)dpi, (float)dpi);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.White);
        DrawPlan(graphics, plan);

        using var stream = new MemoryStream();
        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        return stream.ToArray();
    }

    public byte[] RenderPrintPathPreviewPng(NativeLabelDrawingPlan plan, decimal dpi = 300m)
    {
        var converter = new PrintUnitConverter(dpi);
        var widthPixels = converter.MillimetersToPixels(plan.PaperSize.HeightMm);
        var heightPixels = converter.MillimetersToPixels(plan.PaperSize.WidthMm);

        using var bitmap = new Bitmap(widthPixels, heightPixels);
        bitmap.SetResolution((float)dpi, (float)dpi);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.White);
        DrawPlanPreRotatedForDriver(graphics, plan);

        using var stream = new MemoryStream();
        bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        return stream.ToArray();
    }

    private void DrawPlanForPrint(Graphics graphics, PageSettings pageSettings, NativeLabelDrawingPlan plan, LabelPrintMode mode)
    {
        PrepareGraphics(graphics);
        var state = graphics.Save();
        graphics.TranslateTransform(
            -HundredthsInchToMillimeters(pageSettings.HardMarginX),
            -HundredthsInchToMillimeters(pageSettings.HardMarginY));
        if (mode == LabelPrintMode.FeedPreRotated90)
        {
            DrawPlanPreRotatedForDriver(graphics, plan);
        }
        else
        {
            DrawCommands(graphics, plan);
        }

        graphics.Restore(state);
    }

    private void DrawPlanPreRotatedForDriver(Graphics graphics, NativeLabelDrawingPlan plan)
    {
        PrepareGraphics(graphics);
        var state = graphics.Save();
        graphics.TranslateTransform((float)plan.PaperSize.HeightMm, 0f);
        graphics.RotateTransform(90f);
        DrawCommands(graphics, plan);
        graphics.Restore(state);
    }

    private static float HundredthsInchToMillimeters(float value)
    {
        return value * 0.254f;
    }

    private void DrawCommand(Graphics graphics, NativeDrawCommand command)
    {
        switch (command.Type)
        {
            case NativeDrawCommandType.Text:
                DrawText(graphics, command);
                break;
            case NativeDrawCommandType.QrCode:
                DrawQrCode(graphics, command);
                break;
            case NativeDrawCommandType.Rectangle:
                graphics.DrawRectangle(Pens.Black, (float)command.X, (float)command.Y, (float)command.Width, (float)command.Height);
                break;
        }
    }

    private static void DrawText(Graphics graphics, NativeDrawCommand command)
    {
        using var font = new Font(
            "Microsoft YaHei",
            (float)command.FontSizePt,
            command.Bold ? FontStyle.Bold : FontStyle.Regular,
            GraphicsUnit.Point);
        using var brush = new SolidBrush(Color.Black);
        using var format = new StringFormat
        {
            FormatFlags = StringFormatFlags.NoClip,
            Trimming = command.Ellipsis ? StringTrimming.EllipsisCharacter : StringTrimming.None
        };
        if (command.MaxLines > 0)
        {
            format.FormatFlags |= StringFormatFlags.LineLimit;
        }

        if (command.Height <= 2m)
        {
            format.FormatFlags |= StringFormatFlags.NoWrap;
        }

        var bounds = new RectangleF((float)command.X, (float)command.Y, (float)command.Width, (float)command.Height);
        if (command.RotationDegrees == 0m)
        {
            graphics.DrawString(command.Text, font, brush, bounds, format);
            return;
        }

        var state = graphics.Save();
        if (command.RotationDegrees == 90m)
        {
            graphics.TranslateTransform((float)command.X, (float)(command.Y + command.Height));
            graphics.RotateTransform(270f);
            graphics.DrawString(command.Text, font, brush, new RectangleF(0, 0, (float)command.Height, (float)command.Width), format);
            graphics.Restore(state);
            return;
        }

        graphics.TranslateTransform((float)(command.X + command.Width), (float)(command.Y + command.Height));
        graphics.RotateTransform((float)command.RotationDegrees);
        graphics.DrawString(command.Text, font, brush, new RectangleF(0, 0, (float)command.Width, (float)command.Height), format);
        graphics.Restore(state);
    }

    private void DrawQrCode(Graphics graphics, NativeDrawCommand command)
    {
        var matrix = qrCodeRenderer.Render(command.Text);
        var x = (float)command.X;
        var y = (float)command.Y;
        var width = (float)command.Width;
        var moduleSize = width / matrix.Size;

        using var brush = new SolidBrush(Color.Black);
        graphics.PixelOffsetMode = PixelOffsetMode.Half;
        for (var row = 0; row < matrix.Size; row++)
        {
            for (var column = 0; column < matrix.Size; column++)
            {
                if (!matrix.HasDarkModule(column, row))
                {
                    continue;
                }

                graphics.FillRectangle(
                    brush,
                    x + column * moduleSize,
                    y + row * moduleSize,
                    moduleSize,
                    moduleSize);
            }
        }
    }
}
