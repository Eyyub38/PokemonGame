$scriptDir = "d:\Game_Projects\PokemonProject\Assets\Scripts"

Get-ChildItem -Path $scriptDir -Filter "*.cs" -Recurse | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    $newContent = $content -replace 'FindFirstObjectByType', 'FindAnyObjectByType'
    $newContent = $newContent -replace ',\s*FindObjectsSortMode\.InstanceID', ''
    $newContent = $newContent -replace ',\s*FindObjectsSortMode\.None', ''
    $newContent = $newContent -replace '\(FindObjectsSortMode\.InstanceID\)', '()'
    $newContent = $newContent -replace '\(FindObjectsSortMode\.None\)', '()'

    if ($content -ne $newContent) {
        Set-Content -Path $_.FullName -Value $newContent -NoNewline
        Write-Host "Updated $($_.FullName)"
    }
}
