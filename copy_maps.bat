@echo off
echo Starting Stardew Valley map copy process
echo ---------------------------------------

:: Configuration
set SOURCE_DIR="C:\Users\kanto\OneDrive\Documents\stardew_plugin\game_folder\Stardew Valley\Content (unpacked)\Maps"
set TARGET_DIR="C:\Users\kanto\OneDrive\Documents\stardew_plugin\assets\maps"

:: Make sure target directory exists
if not exist %TARGET_DIR% mkdir %TARGET_DIR%

:: Copy all PNG files from source to target
echo Copying PNG files from %SOURCE_DIR% to %TARGET_DIR%...
xcopy %SOURCE_DIR%\*.png %TARGET_DIR% /Y /I

:: Copy additional PNG files that might be useful for map rendering
echo Copying additional PNG files from Content (unpacked)...
xcopy "C:\Users\kanto\OneDrive\Documents\stardew_plugin\game_folder\Stardew Valley\Content (unpacked)\LooseSprites\Locations\*.png" %TARGET_DIR% /Y /I

echo ---------------------------------------
echo Copy process complete!
echo Maps should be available in %TARGET_DIR%
echo ---------------------------------------
pause