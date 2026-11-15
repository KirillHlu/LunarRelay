# PythonAnywhere Web tab: replace the WSGI file contents with this file.
import os
import sys
from pathlib import Path

# Default: /home/<your-login>/lunar-relay; override LUNAR_RELAY_ROOT if needed.
project = Path(os.environ.get("LUNAR_RELAY_ROOT", str(Path.home() / "lunar-relay"))).resolve()
sys.path.insert(0, str(project))
from app import app as application
