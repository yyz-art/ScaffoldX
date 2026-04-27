$entity = [char]0x26 + 'quot' + [char]0x3B
$quote  = [string][char]0x22

$files = @(
    'D:\VS repos\ScaffoldX\src\ScaffoldX.Core\TemplateProcessing\PostProcessor.cs',
    'D:\VS repos\ScaffoldX\src\ScaffoldX.App\Models\ProjectConfig.cs',
    'D:\VS repos\ScaffoldX\src\ScaffoldX.App\Models\ProjectHistory.cs',
    'D:\VS repos\ScaffoldX\src\ScaffoldX.App\Models\GenerationProgress.cs',
    'D:\VS repos\ScaffoldX\src\ScaffoldX.App\Models\GenerationResult.cs',
    'D:\VS repos\ScaffoldX\src\ScaffoldX.App\Services\IValidationService.cs',
    'D:\VS repos\ScaffoldX\src\ScaffoldX.App\Services\ValidationService.cs',
    'D:\VS repos\ScaffoldX\src\ScaffoldX.App\Services\IHistoryService.cs',
    'D:\VS repos\ScaffoldX\src\ScaffoldX.App\Services\HistoryService.cs',
    'D:\VS repos\ScaffoldX\src\ScaffoldX.App\Services\ITemplateEngine.cs',
    'D:\VS repos\ScaffoldX\src\ScaffoldX.App\Services\ScribanTemplateEngine.cs',
    'D:\VS repos\ScaffoldX\src\ScaffoldX.App\Services\IProjectGenerator.cs',
    'D:\VS repos\ScaffoldX\src\ScaffoldX.App\Services\ProjectGenerator.cs'
)

foreach ($file in $files) {
    if (-not (Test-Path $file)) {
        Write-Output ('SKIP (not found): ' + $file)
        continue
    }
    $text = [System.IO.File]::ReadAllText($file, [System.Text.Encoding]::UTF8)
    $newText = $text.Replace($entity, $quote)
    if ($newText -ne $text) {
        [System.IO.File]::WriteAllText($file, $newText, [System.Text.Encoding]::UTF8)
        Write-Output ('Fixed: ' + $file)
    } else {
        Write-Output ('Clean: ' + $file)
    }
}
