import sqlite3

path = 'test.db'

def create_tables():
    with sqlite3.connect(path) as conn:
        cur = conn.cursor()
        cur.execute("create table if not exists settings (name, value)")
        cur.execute("create table if not exists logs (timestamp, level, message)")
        cur.execute("create table if not exists instances (id, name, status)")
        cur.execute("create table if not exists shared_data (label, data)")

def drop_tables():
    with sqlite3.connect(path) as conn:
        cur = conn.cursor()
        cur.execute("drop table if exists settings")
        cur.execute("drop table if exists logs")
        cur.execute("drop table if exists instances")
        cur.execute("drop table if exists shared_data")

def test_connection() -> bool:
    try:
        with sqlite3.connect(path) as conn:
            cur = conn.cursor()
            cur.execute("select 1")
        return True
    except sqlite3.Error:
        return False

def save_settings(settings: dict[str, str]):
    with sqlite3.connect(path) as conn:
        cur = conn.cursor()
        # Update or insert settings
        for name, value in settings.items():
            cur.execute("insert or replace into settings (name, value) values (?, ?)", (name, value))

def load_settings() -> dict[str, str]:
    with sqlite3.connect(path) as conn:
        cur = conn.cursor()
        cur.execute("select name, value from settings")
        rows = cur.fetchall()
        return {name: value for name, value in rows}

def get_last_instance_id() -> int | None:
    with sqlite3.connect(path) as conn:
        cur = conn.cursor()
        cur.execute("select id from instances order by id desc limit 1")
        row = cur.fetchone()
        return row[0] if row else None

def save_instance(instance_id: int, name: str, status: str):
    with sqlite3.connect(path) as conn:
        cur = conn.cursor()
        cur.execute("insert or replace into instances (id, name, status) values (?, ?, ?)", (instance_id, name, status))

def load_instances() -> list[tuple[int, str, str]]:
    with sqlite3.connect(path) as conn:
        cur = conn.cursor()
        cur.execute("select id, name, status from instances")
        return cur.fetchall()

def save_log(timestamp: str, level: str, message: str):
    with sqlite3.connect(path) as conn:
        cur = conn.cursor()
        cur.execute("insert into logs (timestamp, level, message) values (?, ?, ?)", (timestamp, level, message))

def load_logs() -> list[tuple[str, str, str]]:
    with sqlite3.connect(path) as conn:
        cur = conn.cursor()
        cur.execute("select timestamp, level, message from logs")
        return cur.fetchall()

def save_shared_data(label: str, data: str):
    with sqlite3.connect(path) as conn:
        cur = conn.cursor()
        cur.execute("insert or replace into shared_data (label, data) values (?, ?)", (label, data))

def load_shared_data(label: str) -> str | None:
    with sqlite3.connect(path) as conn:
        cur = conn.cursor()
        cur.execute("select data from shared_data where label = ?", (label,))
        row = cur.fetchone()
        return row[0] if row else None

def load_all_shared_data() -> dict[str, str]:
    with sqlite3.connect(path) as conn:
        cur = conn.cursor()
        cur.execute("select label, data from shared_data")
        rows = cur.fetchall()
        return {label: data for label, data in rows}