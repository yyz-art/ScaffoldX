$file = 'D:\VS repos\ScaffoldX\src\ScaffoldX.Core\TemplateProcessing\PostProcessor.cs'

# Build the problematic line using char codes to avoid any encoding issues
# Target line: .Replace(""", "\"")
# In C#:       .Replace(""", "\"")
# chars:       .Replace( " & q u o t ; " ,   " \ " " )
$dq   = [char]0x22   # "
$bs   = [char]0x5C   # \
$amp  = [char]0x26   # &
$semi = [char]0x3B   # ;

# The search string in the file (what was written by the Write tool):
# .Replace(""", "\"")
# where " is the 6-char HTML entity
$htmlEntity = $amp + 'quot' + $semi   # "

# The correct C# line:
# .Replace(""", "\"")
# first arg:  "  (the 6-char string)
# second arg: \"  (backslash + doublequote)
$correctLine = '            .Replace(' + $dq + $htmlEntity + $dq + ', ' + $dq + $bs + $dq + $dq + ')'

$text = [System.IO.File]::ReadAllText($file, [System.Text.Encoding]::UTF8)
$lines = $text -split "`n"

$fixed = $false
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match 'Replace.*quot.*\\') {
        Write-Output ('Replacing line ' + ($i+1) + ': [' + $lines[$i] + ']')
        $lines[$i] = $correctLine
        $fixed = $true
    }
}

if ($fixed) {
    $newText = $lines -join "`n"
    [System.IO.File]::WriteAllText($file, $newText, [System.Text.Encoding]::UTF8)
    Write-Output 'Saved.'
} else {
    Write-Output 'Line not found - dumping all Replace lines:'
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match 'Replace') {
            Write-Output ('  Line ' + ($i+1) + ': [' + $lines[$i] + ']')
        }
    }
}
