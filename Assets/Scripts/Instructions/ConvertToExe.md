CONVERT PIPELINE.PY TO EXE

To convert your Python scripts into standalone executables and test the full pipeline in Unity, follow these steps.

### 1. Convert the Python Script to an Executable (`.exe`)

You will use **PyInstaller** to bundle your Python script and its dependencies (like pandas and numpy) into a single `.exe` file.

1. Open your Command Prompt or Terminal.
2. Install PyInstaller (if you haven't already) by running:
```bash
pip install pyinstaller

```


3. Navigate to the directory where your `Pipeline_V2.py` and `Pipeline_V3.py` files are saved:
```bash
cd path\to\your\python\scripts

```


4. Run the PyInstaller command with the `--onefile` flag to bundle everything into a single executable:
```bash
pyinstaller --onefile Pipeline_V2.py

```


*(If you also made changes to V3, run `pyinstaller --onefile Pipeline_V3.py` as well).*
5. Once the process completes, look for a newly generated folder named **`dist`** in that directory. Inside `dist`, you will find your `Pipeline_V2.exe`.

### 2. Set Up the Executables in Unity

Your `PythonIntegration.cs` script is programmed to look for these executables in a very specific Unity folder.

1. Open your Unity project.
2. Navigate to your **`Assets`** folder. If you do not have a folder named exactly **`StreamingAssets`**, create one now.
3. Drag and drop `Pipeline_V2.exe` and `Pipeline_V3.exe` from your Python `dist` folder directly into `Assets/StreamingAssets`.
4. Ensure your baseline data (`VisualAccessibility_Baseline.csv`) is placed directly in your root `Assets` folder, as your script uses `Application.dataPath` to locate it.

### 3. Test the Integration in Play Mode

With the updated C# scripts saved and the executables in place, you can test the end-to-end flow.

1. Enter **Play Mode** in Unity and navigate to your Visual Accessibility settings scene.
2. Make some adjustments to the visual sliders and click your **Save** button (which triggers your `ExportToCSV` method and subsequently calls `PythonIntegration.CallPythonScript()`).
3. Open the **Unity Console**. You should see the system executing both pipelines sequentially. Look for the following success logs:
* `Pipeline V2 Output: ...`
* `AI Preset V.A. #2 Saved!`
* `Pipeline V3 Output: ...`
* `AI Preset V.A. #3 Saved!`
* `Both AI Pipelines have finished running and presets are saved.`


4. Navigate to your **Manual Settings UI** scene (or wherever your `S13_ManualSettingsUI` dropdown is located).
5. Open the graphics mode dropdown. You should now see four options: **Default**, **Custom**, **V.A. #2**, and **V.A. #3**.
6. Select **V.A. #2** and then **V.A. #3** to verify that the sliders snap to the distinct AI-generated values for each pipeline version.