from datetime import timezone, datetime, timedelta
import random

import services.instance_controller as instance_controller
from flask import Blueprint, render_template, url_for, redirect, flash, request

dashboard_bp = Blueprint('dashboard', __name__)

@dashboard_bp.route('/')
def index():
    metrics = [
        {"title": "Active Users", "value": 1234, "delta": 4.2},
        {"title": "Errors", "value": 12, "delta": -1.1},
        {"title": "Throughput", "value": "540 req/s", "delta": 8.0},
        {"title": "Latency (p95)", "value": "120 ms", "delta": 0},
    ]

    # Chart data (last 24 points as example)
    now = datetime.now(timezone.utc) + timedelta(hours=1)
    next_hour = (now + timedelta(hours=1)).replace(minute=0, second=0, microsecond=0)
    chart_labels = [(next_hour - timedelta(hours=i)).time().isoformat('minutes') for i in reversed(range(24))]
    chart_values = [random.randint(200, 800) for _ in range(24)]

    presets = instance_controller.instance_presets.keys()
    running_instances = [
        { "time": "2025-11-19 10:00", "preset": "fast", "status": "running", "location": "Living Room" },
    ]
    activity = [{"time": "10:05", "text": "Deployed preset fast"}]

    return render_template(
        "overview.html",
        metrics=metrics,
        chart_labels=chart_labels,
        chart_values=chart_values,
        presets=presets,
        running_instances=running_instances,
        activity=activity,
    )

@dashboard_bp.route('/settings')
def settings():
    with open('user_settings.cfg', 'r') as f:
        lines = f.readlines()
        data = {}
        for line in lines:
            key, value = line.strip().split('=')
            data[key] = value
    return render_template("settings.html", data=data)

@dashboard_bp.route('/settings/save', methods=['POST'])
def save_settings():
    with open('user_settings.cfg', 'w') as f:
        for k, v in request.form.items():
            f.write(f"{k}={v}\n")

    return redirect(url_for('dashboard.settings'))