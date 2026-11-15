# Download website
Flask source for https://lunarrelay1.pythonanywhere.com/ .
Run `python fetch_releases.py` to retrieve the release binaries (verified against releases.json), then `pip install -r requirements.txt` and `python app.py`.
For PythonAnywhere place this folder at `~/lunar-relay` and use wsgi.py as the Web app WSGI entry. Map `/static/` to this folder's static directory. Use a matching Python virtual environment. Tests: `python -m unittest -v` after fetching releases.
