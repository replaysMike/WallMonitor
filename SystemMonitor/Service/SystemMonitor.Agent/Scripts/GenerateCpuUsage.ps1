$NumberOfLogicalProcessors = Get-WmiObject win32_processor | Select-Object -ExpandProperty NumberOfLogicalProcessors

ForEach ($core in 1..$NumberOfLogicalProcessors){ 

start-job -ScriptBlock{

    $result = 1;
    foreach ($loopnumber in 1..2147483647){
        $result=1;
        
        foreach ($loopnumber1 in 1..2147483647){
        $result=1;
            
            foreach($number in 1..2147483647){
                $result = $result * $number
            }
        }

            $result
        }
    }
}

Read-Host "Press any key to exit..."
Stop-Job * 