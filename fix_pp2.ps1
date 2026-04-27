# Fix PostProcessor.cs line 379: replace .Replace("\"", """) with .Replace("\"", """)
$file = 'D:\VS repos\ScaffoldX\src\ScaffoldX.Core\TemplateProcessing\PostProcessor.cs'

$bytes = [System.IO.File]::ReadAllBytes($file)

# needle:  2E 52 65 70 6C 61 63 65 28 22 5C 22 22 2C 20 22 22 22 29
# = .Replace("\"", """)
$needle = [byte[]](0x2E,0x52,0x65,0x70,0x6C,0x61,0x63,0x65,0x28,
                   0x22,0x5C,0x22,0x22,0x2C,0x20,
                   0x22,0x22,0x22,0x29)

# replacement: 2E 52 65 70 6C 61 63 65 28 22 5C 22 22 2C 20 22 26 71 75 6F 74 3B 22 29
# = .Replace("\"", """)
$replacement = [byte[]](0x2E,0x52,0x65,0x70,0x6C,0x61,0x63,0x65,0x28,
                        0x22,0x5C,0x22,0x22,0x2C,0x20,
                        0x22,0x26,0x71,0x75,0x6F,0x74,0x3B,0x22,0x29)

# Search for needle in bytes
$found = -1
for ($i = 0; $i -le $bytes.Length - $needle.Length; $i++) {
    $match = $true
    for ($j = 0; $j -lt $needle.Length; $j++) {
        if ($bytes[$i + $j] -ne $needle[$j]) { $match = $false; break }
    }
    if ($match) { $found = $i; break }
}

if ($found -ge 0) {
    $newBytes = New-Object System.Collections.Generic.List[byte]
    $newBytes.AddRange($bytes[0..($found-1)])
    $newBytes.AddRange($replacement)
    $newBytes.AddRange($bytes[($found + $needle.Length)..($bytes.Length - 1)])
    [System.IO.File]::WriteAllBytes($file, $newBytes.ToArray())
    Write-Output 'Fixed successfully'
} else {
    Write-Output 'Needle not found'
    exit 1
}
