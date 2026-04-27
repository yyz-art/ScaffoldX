$file = 'D:\VS repos\ScaffoldX\src\ScaffoldX.Core\TemplateProcessing\PostProcessor.cs'
$content = [System.IO.File]::ReadAllText($file, [System.Text.Encoding]::UTF8)

# Bad line:  .Replace("\"", """)
# The replacement value is three double-quote chars followed by )
# Correct:   .Replace("\"", """)

$needle  = ".Replace(`"\`"`", `"`"`"`")"
$replace = ".Replace(`"\`"`", `""`")"

if ($content.Contains($needle)) {
    $fixed = $content.Replace($needle, $replace)
    [System.IO.File]::WriteAllText($file, $fixed, [System.Text.Encoding]::UTF8)
    Write-Host "Fixed successfully"
} else {
    # Fallback: build needle from raw char codes to avoid any escaping ambiguity
    # .Replace("\"",  """  )
    # chars:  2E 52 65 70 6C 61 63 65 28 22 5C 22 22 2C 20 22 22 22 29
    $needleBytes  = [byte[]](0x2E,0x52,0x65,0x70,0x6C,0x61,0x63,0x65,0x28,
                             0x22,0x5C,0x22,0x22,0x2C,0x20,
                             0x22,0x22,0x22,0x29)
    $replaceBytes = [byte[]](0x2E,0x52,0x65,0x70,0x6C,0x61,0x63,0x65,0x28,
                             0x22,0x5C,0x22,0x22,0x2C,0x20,
                             0x22,0x26,0x71,0x75,0x6F,0x74,0x3B,0x22,0x29)

    $needleStr  = [System.Text.Encoding]::UTF8.GetString($needleBytes)
    $replaceStr = [System.Text.Encoding]::UTF8.GetString($replaceBytes)

    if ($content.Contains($needleStr)) {
        $fixed = $content.Replace($needleStr, $replaceStr)
        [System.IO.File]::WriteAllText($file, $fixed, [System.Text.Encoding]::UTF8)
        Write-Host "Fixed via byte-needle successfully"
    } else {
        Write-Host "ERROR: needle not found in file"
        exit 1
    }
}
