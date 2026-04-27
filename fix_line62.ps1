$file = 'D:\VS repos\ScaffoldX\src\ScaffoldX.Core\TemplateProcessing\PostProcessor.cs'
$dq   = [char]0x22
$bs   = [char]0x5C
$amp  = [char]0x26
$semi = [char]0x3B

$htmlEntity = $amp + 'quot' + $semi

$text  = [System.IO.File]::ReadAllText($file, [System.Text.Encoding]::UTF8)
$lines = $text -split "`n"

Write-Output ('Line 62 raw bytes: ' + [System.BitConverter]::ToString([System.Text.Encoding]::UTF8.GetBytes($lines[61])))

$correctLine = '            .Replace(' + $dq + $htmlEntity + $dq + ', ' + $dq + $bs + $dq + $dq + ')'
Write-Output ('Correct line: [' + $correctLine + ']')

$lines[61] = $correctLine
$newText = $lines -join "`n"
[System.IO.File]::WriteAllText($file, $newText, [System.Text.Encoding]::UTF8)
Write-Output 'Line 62 replaced and file saved.'
