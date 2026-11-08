Got it! Since your CSV files are located in the Unity project root, alongside the `Assets` folder, your folder structure should look like this:

```
Thesis-New-Game/
│
├── Assets/
│   ├── Scripts/
│   │   └── PythonScripts/
│   │       └── Pipeline_V3.py
│   │
│   └── StreamingAssets/
│       └── Pipeline_V3.exe
│
├── VisualAccessibility_Baseline.csv
└── VisualAccessibility_Player.csv
```

Let's test both versions using the same CSV files.

## 1. Test the Python script (.py)

Open your VS Code terminal and execute:

```
cd "C:\PROGRAMMING\GAME DEV\Thesis-New-Game"
```

Then run:

```
python ".\Assets\Scripts\PythonScripts\Pipeline_V3.py" ".\VisualAccessibility_Baseline.csv" ".\VisualAccessibility_Player.csv"
```

If `python` doesn't work, try:

```
py ".\Assets\Scripts\PythonScripts\Pipeline_V3.py" ".\VisualAccessibility_Baseline.csv" ".\VisualAccessibility_Player.csv"
```

## 2. Test the standalone executable (.exe)

Stay in the same project root directory and execute:

```
.\Assets\StreamingAssets\Pipeline_V3.exe --mode all --baseline ".\Assets\VisualAccessibility_Baseline.csv" --player ".\Assets\VisualAccessibility_Player.csv"
```

This tests whether the EXE can locate and process the CSV files using relative paths.

## 3. Compare the results

Both versions should process the same input files.

| Validation              | Expected               |
| ----------------------- | ---------------------- |
| Baseline CSV records    | 30                     |
| Player CSV records used | 1                      |
| Total records           | 31                     |
| CSV columns             | 14                     |
| Prediction              | Successfully generated |
| JSON output             | `AI_RESULT:`           |

## 4. Test both automatically

If you want to run both tests one after another, copy this entire block into PowerShell:

```
$Project = "C:\PROGRAMMING\GAME DEV\Thesis-New-Game"

$PythonScript = "$Project\Assets\Scripts\PythonScripts\Pipeline_V3.py"
$Executable = "$Project\Assets\StreamingAssets\Pipeline_V3.exe"

$Baseline = "$Project\VisualAccessibility_Baseline.csv"
$Player = "$Project\VisualAccessibility_Player.csv"

Write-Host "`n========== PYTHON TEST ==========" -ForegroundColor Cyan

python $PythonScript $Baseline $Player

$PythonExit = $LASTEXITCODE

Write-Host "`n========== EXE TEST ==========" -ForegroundColor Cyan

& $Executable $Baseline $Player

$ExeExit = $LASTEXITCODE

Write-Host "`n========== TEST SUMMARY ==========" -ForegroundColor Yellow

Write-Host "Python Exit Code: $PythonExit"
Write-Host "EXE Exit Code: $ExeExit"
```

Recommended testing sequence

1. Run the Python version.
2. Run the EXE version.
3. Compare their `AI_RESULT:` JSON outputs.
4. Check that both exit codes are `0`.

Important: An exit code of `0` only confirms successful execution, not necessarily identical predictions. We should compare the actual JSON outputs too.

You can send me the terminal outputs from both tests, and I'll help verify whether your Python and EXE implementations produce consistent results.