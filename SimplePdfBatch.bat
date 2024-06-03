@echo off
set "wkhtmltopdf=C:\Program Files\wkhtmltopdf\bin\wkhtmltopdf.exe"
set "sourceDirectory=C:\source\Workspaces\Development\C# Projects\ServiceNowXMLToHTML\OutputData"

for /r "%sourceDirectory%" %%F in (*.html) do (
    echo Processing "%%F"...
    "%wkhtmltopdf%" "%%F" "%%~dpnF.pdf"
    if exist "%%~dpnF.pdf" (
        echo Conversion successful. Deleting "%%F"...
        del "%%F"
    ) else (
        echo Error: Conversion failed for "%%F".
    )
)

echo Conversion and cleanup completed.