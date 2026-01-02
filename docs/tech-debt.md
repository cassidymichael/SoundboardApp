# Technical Debt

Items to address for improved code quality, maintainability, or polish.

---

## Application Icon

**Current state:** The app icon is generated programmatically using GDI+ (System.Drawing) at runtime. The same generated icon is converted and used for both the system tray and window title bar.

**Issue:** This approach is verbose, platform-specific, and makes it harder to maintain consistent branding across different sizes.

**Recommended fix:** Create a proper `.ico` file with multiple sizes (16x16, 32x32, 48x48, 256x256) and use it directly:
- Set as project icon in `.csproj` for the EXE
- Reference in `MainWindow.xaml` via `Icon="pack://application:,,,/Resources/app.ico"`
- Load for tray icon via `new Icon(Application.GetResourceStream(...).Stream)`

**Benefits:**
- Cleaner code (remove `CreateTrayIcon()` GDI+ generation)
- Consistent quality at all sizes
- Standard approach, better tooling support
- Smaller runtime overhead
