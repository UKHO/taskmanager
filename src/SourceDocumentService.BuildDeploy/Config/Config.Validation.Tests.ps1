function Validate-Configuration {
    param(
        [Parameter(Mandatory=$true)]
        $ConfigData
    )

    $ret = @()

    $ret += Validate-NodeData -ConfigData $ConfigData

    if($ret.Count -gt 0){
        $ret | ForEach-Object {
            Write-Error $_ -Category InvalidData
        }
        throw "Errors occurred while validating the configuration data."
    }
}

function Validate-NodeData {
    param(
        [Parameter(Mandatory=$true)]
        $ConfigData
    )

    $mandatoryProperties = @(
        @{ 
            Name = "NodeName"
            TypeName = "String"  
        },
        @{
            Name = "Roles"
            TypeName = "String[]"
        }

    )

    $ret = @()
    if($ConfigData.AllNodes) {
        $ConfigData.AllNodes | ForEach-Object {
            $node = $_
            $mandatoryProperties | ForEach-Object {
                if($node.($_.Name) -eq $null) {
                    $ret += "Node $($node.NodeName) - $($_.Name) is a required field but has a null value."
                } else {
                    #Do clever things wit the type e.g. if string then isnullorempty, if array enusre there are elements etc.
                    if($node.($_.Name).GetType().Name -ne $_.TypeName) {
                        $ret += "Node $($node.NodeName) has an invalid type for $($_.Name). Expected: $($_.TypeName) Actual: $($node.($_.Name).GetType().Name)"
                    }
                }     
            }     
        }
    }

    return $ret
}
