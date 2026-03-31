$neutralPath = 'src/DiskSlim/Strings/Resources.resw'
$zhPath = 'src/DiskSlim/Strings/zh-CN/Resources.resw'
$enPath = 'src/DiskSlim/Strings/en-US/Resources.resw'

[xml]$neutral = Get-Content $neutralPath
[xml]$zh = Get-Content $zhPath
[xml]$en = Get-Content $enPath

function Get-Map($doc)
{
    $map = @{}
    foreach ($d in @($doc.root.data))
    {
        $name = [string]$d.name
        $valueNode = $d.SelectSingleNode('value')
        $value = if ($null -ne $valueNode) { [string]$valueNode.InnerText } else { '' }
        if (-not [string]::IsNullOrWhiteSpace($name))
        {
            $map[$name] = $value
        }
    }
    return $map
}

function Set-DataNodes($doc, $targetMap)
{
    foreach ($node in @($doc.root.data))
    {
        [void]$doc.root.RemoveChild($node)
    }

    foreach ($k in ($targetMap.Keys | Sort-Object))
    {
        $data = $doc.CreateElement('data')

        $nameAttr = $doc.CreateAttribute('name')
        $nameAttr.Value = $k
        [void]$data.Attributes.Append($nameAttr)

        $spaceAttr = $doc.CreateAttribute('xml', 'space', 'http://www.w3.org/XML/1998/namespace')
        $spaceAttr.Value = 'preserve'
        [void]$data.Attributes.Append($spaceAttr)

        $valueNode = $doc.CreateElement('value')
        $valueNode.InnerText = [string]$targetMap[$k]
        [void]$data.AppendChild($valueNode)

        [void]$doc.root.AppendChild($data)
    }
}

$neutralMap = Get-Map $neutral
$zhMap = Get-Map $zh
$enMap = Get-Map $en

$allKeys = @($neutralMap.Keys + $zhMap.Keys + $enMap.Keys | Sort-Object -Unique)

$newNeutralMap = @{}
$newZhMap = @{}
$newEnMap = @{}

foreach ($k in $allKeys)
{
    $zhVal = if ($zhMap.ContainsKey($k)) { $zhMap[$k] } else { '' }
    $neutralVal = if ($neutralMap.ContainsKey($k)) { $neutralMap[$k] } else { '' }
    $enVal = if ($enMap.ContainsKey($k)) { $enMap[$k] } else { '' }

    $finalNeutral = if (-not [string]::IsNullOrWhiteSpace($zhVal)) { $zhVal } elseif (-not [string]::IsNullOrWhiteSpace($neutralVal)) { $neutralVal } else { $enVal }
    $finalZh = if (-not [string]::IsNullOrWhiteSpace($zhVal)) { $zhVal } else { $finalNeutral }
    $finalEn = if (-not [string]::IsNullOrWhiteSpace($enVal)) { $enVal } else { $finalNeutral }

    $newNeutralMap[$k] = $finalNeutral
    $newZhMap[$k] = $finalZh
    $newEnMap[$k] = $finalEn
}

Set-DataNodes $neutral $newNeutralMap
Set-DataNodes $zh $newZhMap
Set-DataNodes $en $newEnMap

$neutral.Save((Resolve-Path $neutralPath))
$zh.Save((Resolve-Path $zhPath))
$en.Save((Resolve-Path $enPath))

"Synced keys: $($allKeys.Count)"
"zh-CN translated from zh map: $(@($zhMap.Keys).Count)"
