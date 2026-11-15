from pathlib import Path
import json
from flask import Flask, render_template, send_from_directory, abort

ROOT = Path(__file__).resolve().parent
app = Flask(__name__)
app.config.update(MAX_CONTENT_LENGTH=1024 * 1024, SEND_FILE_MAX_AGE_DEFAULT=3600)
RELEASES = json.loads((ROOT / "releases.json").read_text(encoding="utf-8"))

@app.get("/")
def index():
    return render_template("index.html", releases=RELEASES)

@app.get("/download/<platform>")
def download(platform):
    release = RELEASES.get(platform)
    if not release:
        abort(404)
    return send_from_directory(ROOT / "downloads", release["file"], as_attachment=True,
                               download_name=release["file"], conditional=True,
                               etag=release["sha256"], max_age=0)

@app.get("/checksums.txt")
def checksums():
    text = "".join(f'{v["sha256"]}  {v["file"]}\n' for v in RELEASES.values())
    return app.response_class(text, mimetype="text/plain")

@app.after_request
def headers(response):
    response.headers["X-Content-Type-Options"] = "nosniff"
    response.headers["Referrer-Policy"] = "strict-origin-when-cross-origin"
    response.headers["X-Frame-Options"] = "SAMEORIGIN"
    response.headers["Content-Security-Policy"] = "default-src 'self'; img-src 'self' data:; style-src 'self'; script-src 'self'; font-src 'self'; object-src 'none'; base-uri 'self'; frame-ancestors 'self'"
    return response

@app.errorhandler(404)
def missing(_):
    return render_template("404.html"), 404

if __name__ == "__main__":
    app.run(host="127.0.0.1", port=5074, debug=False)
