$file = 'D:\VS repos\ScaffoldX\src\ScaffoldX.Core\TemplateProcessing\PostProcessor.cs'
$text = [System.IO.File]::ReadAllText($file, [System.Text.Encoding]::UTF8)

$entity = [char]0x26 + 'quot' + [char]0x3B
$quote  = [string][char]0x22

$newText = $text.Replace($entity, $quote)

if ($newText -ne $text) {
    [System.IO.File]::WriteAllText($file, $newText, [System.Text.Encoding]::UTF8)
    Write-Output 'Replaced &quot; entities with literal double-quotes. File saved.'
} else {
    Write-Output 'No &quot; entities found in file.'
}
