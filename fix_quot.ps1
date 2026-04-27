$file = 'D:\VS repos\ScaffoldX\src\ScaffoldX.Core\TemplateProcessing\PostProcessor.cs'
$text = [System.IO.File]::ReadAllText($file, [System.Text.Encoding]::UTF8)

$lines = $text -split "`n"
$changed = $false

for ($i = 0; $i -lt $lines.Count; $i++) {
    $line = $lines[$i]
    if ($line.Contains('"')) {
        $fixed = $line.Replace('"', '"')
        $lines[$i] = $fixed
        $changed = $true
        Write-Output ("Fixed line " + ($i+1) + ": " + $fixed)
    }
}

if ($changed) {
    $newText = $lines -join "`n"
    [System.IO.File]::WriteAllText($file, $newText, [System.Text.Encoding]::UTF8)
    Write-Output "File saved."
} else {
    Write-Output "No " entities found."
}
