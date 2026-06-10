using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace TargetFishSync;

internal sealed class TargetFishMenu : IClickableMenu
{
    private const int HeaderHeight = 172;
    private const int CardWidth = 164;
    private const int CardHeight = 164;
    private const int CardGap = 12;
    private const int ContentPadding = 36;
    private const int NameAreaHeight = 58;

    private readonly List<FishEntry> OriginalFish;
    private List<FishEntry> VisibleFish = new();
    private readonly FishQuality Quality;
    private readonly Action<FishEntry> SelectFish;
    private readonly Action Disable;
    private readonly ITranslationHelper Translation;
    private readonly Action<CatchSortMode> SortChanged;
    private readonly Action<CatchFilterMode> FilterChanged;
    private readonly int Columns;
    private readonly int VisibleRows;
    private readonly Rectangle SortButtonBounds;
    private readonly Rectangle FilterButtonBounds;
    private CatchSortMode SortMode;
    private CatchFilterMode FilterMode;
    private int ScrollRow;

    public TargetFishMenu(
        List<FishEntry> fish,
        FishQuality quality,
        Action<FishEntry> selectFish,
        Action disable,
        CatchSortMode sortMode,
        CatchFilterMode filterMode,
        Action<CatchSortMode> sortChanged,
        Action<CatchFilterMode> filterChanged,
        ITranslationHelper translation)
        : base(
            GetMenuX(),
            GetMenuY(),
            GetMenuWidth(),
            GetMenuHeight(),
            true)
    {
        OriginalFish = fish;
        Quality = quality;
        SelectFish = selectFish;
        Disable = disable;
        SortMode = sortMode;
        FilterMode = filterMode;
        SortChanged = sortChanged;
        FilterChanged = filterChanged;
        Translation = translation;
        Columns = Math.Max(1, (width - ContentPadding * 2 + CardGap) / (CardWidth + CardGap));
        VisibleRows = Math.Max(1, (height - HeaderHeight - ContentPadding) / (CardHeight + CardGap));
        var controlY = yPositionOnScreen + 108;
        var controlWidth = (width - ContentPadding * 2 - CardGap) / 2;
        SortButtonBounds = new Rectangle(xPositionOnScreen + ContentPadding, controlY, controlWidth, 48);
        FilterButtonBounds = new Rectangle(SortButtonBounds.Right + CardGap, controlY, controlWidth, 48);
        RefreshVisibleFish();
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (x < xPositionOnScreen
            || x >= xPositionOnScreen + width
            || y < yPositionOnScreen
            || y >= yPositionOnScreen + height)
        {
            exitThisMenu();
            return;
        }

        if (SortButtonBounds.Contains(x, y))
        {
            SortMode = (CatchSortMode)(((int)SortMode + 1) % Enum.GetValues<CatchSortMode>().Length);
            SortChanged(SortMode);
            RefreshVisibleFish();
            Game1.playSound("shwip");
            return;
        }

        if (FilterButtonBounds.Contains(x, y))
        {
            FilterMode = (CatchFilterMode)(((int)FilterMode + 1) % Enum.GetValues<CatchFilterMode>().Length);
            FilterChanged(FilterMode);
            RefreshVisibleFish();
            Game1.playSound("shwip");
            return;
        }

        var index = GetEntryIndexAt(x, y);
        if (index < 0)
        {
            return;
        }

        if (index < VisibleFish.Count)
        {
            SelectFish(VisibleFish[index]);
            Game1.playSound("smallSelect");
            exitThisMenu();
            return;
        }

        if (index == VisibleFish.Count)
        {
            Disable();
            Game1.playSound("smallSelect");
            exitThisMenu();
        }
    }

    public override void receiveScrollWheelAction(int direction)
    {
        var maxScrollRow = GetMaxScrollRow();
        if (direction < 0)
        {
            ScrollRow = Math.Min(maxScrollRow, ScrollRow + 1);
        }
        else if (direction > 0)
        {
            ScrollRow = Math.Max(0, ScrollRow - 1);
        }
    }

    public override void draw(SpriteBatch b)
    {
        IClickableMenu.drawTextureBox(
            b,
            xPositionOnScreen,
            yPositionOnScreen,
            width,
            height,
            Color.White);

        SpriteText.drawString(
            b,
            Translation.Get("menu.title"),
            xPositionOnScreen + ContentPadding,
            yPositionOnScreen + 20);

        b.DrawString(
            Game1.smallFont,
            Translation.Get("menu.quality", new { quality = Quality }),
            new Vector2(xPositionOnScreen + ContentPadding + 4, yPositionOnScreen + 72),
            Game1.textColor);

        DrawControl(b, SortButtonBounds, Translation.Get("menu.sort", new { mode = GetSortLabel() }));
        DrawControl(b, FilterButtonBounds, Translation.Get("menu.filter", new { mode = GetFilterLabel() }));

        if (VisibleFish.Count == 0)
        {
            var emptyText = OriginalFish.Count > 0 && FilterMode == CatchFilterMode.UncaughtOnly
                ? Translation.Get("menu.empty.uncaught")
                : Translation.Get("menu.empty");
            b.DrawString(
                Game1.smallFont,
                Game1.parseText(emptyText, Game1.smallFont, width - ContentPadding * 2),
                new Vector2(xPositionOnScreen + ContentPadding, yPositionOnScreen + HeaderHeight + 8),
                Game1.textColor);
        }

        var firstIndex = ScrollRow * Columns;
        var visibleCount = VisibleRows * Columns;
        FishEntry? hoveredEntry = null;
        for (var visibleIndex = 0; visibleIndex < visibleCount; visibleIndex++)
        {
            var index = firstIndex + visibleIndex;
            if (index > VisibleFish.Count)
            {
                break;
            }

            var row = visibleIndex / Columns;
            var column = visibleIndex % Columns;
            var bounds = GetCardBounds(row, column);
            var hovered = bounds.Contains(Game1.getMouseX(), Game1.getMouseY());
            var tint = hovered ? Color.Wheat : Color.White;
            IClickableMenu.drawTextureBox(b, bounds.X, bounds.Y, bounds.Width, bounds.Height, tint);

            if (index == VisibleFish.Count)
            {
                DrawCenteredWrappedText(
                    b,
                    Translation.Get("menu.disable"),
                    bounds,
                    Color.DarkRed,
                    bounds.Y + 48,
                    bounds.Height - 64);
                continue;
            }

            var entry = VisibleFish[index];
            if (hovered)
            {
                hoveredEntry = entry;
            }
            entry.PreviewItem.drawInMenu(
                b,
                new Vector2(bounds.Center.X - 32, bounds.Y + 18),
                1f,
                1f,
                0.9f,
                StackDrawType.Hide,
                Color.White,
                true);

            if (entry.CanTrackCaughtStatus && !entry.IsCaught)
            {
                DrawUncaughtDot(b, bounds);
            }

            DrawCenteredWrappedText(
                b,
                entry.DisplayName,
                bounds,
                Game1.textColor,
                bounds.Bottom - NameAreaHeight - 8,
                NameAreaHeight);
        }

        DrawScrollHint(b);
        if (hoveredEntry is not null)
        {
            DrawEntryTooltip(b, hoveredEntry);
        }
        drawMouse(b);
    }

    private int GetEntryIndexAt(int x, int y)
    {
        var contentX = x - (xPositionOnScreen + ContentPadding);
        var contentY = y - (yPositionOnScreen + HeaderHeight);
        if (contentX < 0 || contentY < 0)
        {
            return -1;
        }

        var column = contentX / (CardWidth + CardGap);
        var row = contentY / (CardHeight + CardGap);
        if (column >= Columns || row >= VisibleRows)
        {
            return -1;
        }

        var bounds = GetCardBounds(row, column);
        return bounds.Contains(x, y)
            ? (ScrollRow + row) * Columns + column
            : -1;
    }

    private Rectangle GetCardBounds(int row, int column)
    {
        return new Rectangle(
            xPositionOnScreen + ContentPadding + column * (CardWidth + CardGap),
            yPositionOnScreen + HeaderHeight + row * (CardHeight + CardGap),
            CardWidth,
            CardHeight);
    }

    private int GetMaxScrollRow()
    {
        var totalRows = (int)Math.Ceiling((VisibleFish.Count + 1) / (double)Columns);
        return Math.Max(0, totalRows - VisibleRows);
    }

    private void RefreshVisibleFish()
    {
        VisibleFish = CatchListOrganizer.Apply(
            OriginalFish,
            SortMode,
            FilterMode,
            entry => entry.DisplayName,
            entry => entry.Price,
            entry => entry.CanTrackCaughtStatus,
            entry => entry.IsCaught);
        ScrollRow = 0;
    }

    private void DrawControl(SpriteBatch b, Rectangle bounds, string text)
    {
        var hovered = bounds.Contains(Game1.getMouseX(), Game1.getMouseY());
        IClickableMenu.drawTextureBox(b, bounds.X, bounds.Y, bounds.Width, bounds.Height, hovered ? Color.Wheat : Color.White);
        var parsed = Game1.parseText(text, Game1.smallFont, bounds.Width - 24);
        var size = Game1.smallFont.MeasureString(parsed);
        b.DrawString(Game1.smallFont, parsed, new Vector2(bounds.Center.X - size.X / 2, bounds.Center.Y - size.Y / 2), Game1.textColor);
    }

    private string GetSortLabel()
    {
        return Translation.Get(SortMode switch
        {
            CatchSortMode.PriceHighToLow => "menu.sort.priceHigh",
            CatchSortMode.PriceLowToHigh => "menu.sort.priceLow",
            CatchSortMode.UncaughtFirst => "menu.sort.uncaught",
            _ => "menu.sort.name"
        });
    }

    private string GetFilterLabel()
    {
        return Translation.Get(FilterMode == CatchFilterMode.UncaughtOnly
            ? "menu.filter.uncaught"
            : "menu.filter.all");
    }

    private static void DrawUncaughtDot(SpriteBatch b, Rectangle cardBounds)
    {
        var outline = new Rectangle(cardBounds.X + 10, cardBounds.Y + 10, 16, 16);
        var dot = new Rectangle(outline.X + 3, outline.Y + 3, 10, 10);
        b.Draw(Game1.staminaRect, outline, Color.DarkGreen);
        b.Draw(Game1.staminaRect, dot, Color.LimeGreen);
    }

    private void DrawEntryTooltip(SpriteBatch b, FishEntry entry)
    {
        var lines = new List<string>
        {
            entry.DisplayName,
            Translation.Get("menu.tooltip.price", new { price = entry.Price })
        };
        if (entry.CanTrackCaughtStatus && !entry.IsCaught)
        {
            lines.Add(Translation.Get("menu.tooltip.uncaught"));
        }

        IClickableMenu.drawHoverText(b, string.Join(Environment.NewLine, lines), Game1.smallFont);
    }

    private static void DrawCenteredWrappedText(
        SpriteBatch b,
        string text,
        Rectangle bounds,
        Color color,
        int y,
        int maxHeight)
    {
        var maxWidth = bounds.Width - 20;
        var scale = 1f;
        var lines = WrapText(text, maxWidth, scale);

        while ((lines.Count > 2 
                || GetTextHeight(lines, scale) > maxHeight 
                || lines.Any(line => Game1.smallFont.MeasureString(line).X * scale > maxWidth)) 
               && scale > 0.5f)
        {
            scale -= 0.04f;
            lines = WrapText(text, maxWidth, scale);
        }

        if (lines.Count > 2)
        {
            lines = lines.Take(2).ToList();
            lines[1] = TrimToWidth(lines[1], maxWidth, scale);
        }

        var textHeight = GetTextHeight(lines, scale);
        var lineY = y + Math.Max(0, (maxHeight - textHeight) / 2);
        foreach (var line in lines)
        {
            var size = Game1.smallFont.MeasureString(line) * scale;
            b.DrawString(
                Game1.smallFont,
                line,
                new Vector2(bounds.Center.X - size.X / 2, lineY),
                color,
                0f,
                Vector2.Zero,
                scale,
                SpriteEffects.None,
                1f);
            lineY += (int)size.Y;
        }
    }

    private static List<string> WrapText(string text, int maxWidth, float scale)
    {
        var lines = new List<string>();
        var current = string.Empty;

        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = string.IsNullOrEmpty(current) ? word : current + " " + word;
            if (Game1.smallFont.MeasureString(candidate).X * scale <= maxWidth)
            {
                current = candidate;
                continue;
            }

            if (!string.IsNullOrEmpty(current))
            {
                lines.Add(current);
            }

            current = word;
        }

        if (!string.IsNullOrEmpty(current))
        {
            lines.Add(current);
        }

        return lines.Count > 0 ? lines : new List<string> { text };
    }

    private static string TrimToWidth(string text, int maxWidth, float scale)
    {
        const string suffix = "...";
        while (text.Length > 1 && Game1.smallFont.MeasureString(text + suffix).X * scale > maxWidth)
        {
            text = text[..^1];
        }

        return text.TrimEnd() + suffix;
    }

    private static int GetTextHeight(IEnumerable<string> lines, float scale)
    {
        return (int)(lines.Sum(line => Game1.smallFont.MeasureString(line).Y) * scale);
    }

    private void DrawScrollHint(SpriteBatch b)
    {
        var maxScrollRow = GetMaxScrollRow();
        if (maxScrollRow == 0)
        {
            return;
        }

        var x = xPositionOnScreen + width - 24;
        var top = yPositionOnScreen + HeaderHeight + 8;
        var bottom = yPositionOnScreen + height - 36;
        b.Draw(Game1.staminaRect, new Rectangle(x, top, 4, bottom - top), Color.SaddleBrown * 0.35f);

        var thumbHeight = Math.Max(32, (bottom - top) * VisibleRows / (VisibleRows + maxScrollRow));
        var thumbY = top + (bottom - top - thumbHeight) * ScrollRow / maxScrollRow;
        b.Draw(Game1.staminaRect, new Rectangle(x - 2, thumbY, 8, thumbHeight), Color.SaddleBrown);
    }

    private static int GetMenuWidth()
    {
        return Math.Min(960, Math.Max(360, Game1.uiViewport.Width - 64));
    }

    private static int GetMenuHeight()
    {
        return Math.Min(760, Math.Max(420, Game1.uiViewport.Height - 64));
    }

    private static int GetMenuX()
    {
        return (Game1.uiViewport.Width - GetMenuWidth()) / 2;
    }

    private static int GetMenuY()
    {
        return (Game1.uiViewport.Height - GetMenuHeight()) / 2;
    }
}
