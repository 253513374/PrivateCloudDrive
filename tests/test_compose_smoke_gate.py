from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "scripts" / "smoke-compose-local.sh"
COMPOSE = ROOT / "docker-compose.yml"


def test_compose_smoke_script_guards_swagger_json_and_mobile_token_without_printing_tokens():
    text = SCRIPT.read_text(encoding="utf-8")

    assert "/swagger/v1/swagger.json" in text
    assert "PrivateCloudDrive_App" in text
    assert "/connect/token" in text
    assert "access_token" in text
    assert "token value not printed" in text
    assert "jq" not in text, "script must stay dependency-light for CI/local bash runners"


def test_compose_smoke_script_supports_isolated_ports_and_project_cleanup():
    text = SCRIPT.read_text(encoding="utf-8")

    assert "API_HTTP_PORT" in text
    assert "POSTGRES_PORT" in text
    assert "REDIS_PORT" in text
    assert "COMPOSE_PROJECT_NAME" in text
    assert "docker compose down -v --remove-orphans" in text


def test_compose_config_injects_mobile_client_id_into_runtime_services():
    compose = COMPOSE.read_text(encoding="utf-8")

    expected = "OpenIddict__Applications__PrivateCloudDrive_App__ClientId: ${MOBILE_APP_CLIENT_ID:-PrivateCloudDrive_App}"
    assert compose.count(expected) == 3
    assert '"${API_HTTP_PORT:-8080}:8080"' in compose
    assert '"${POSTGRES_PORT:-5432}:5432"' in compose
    assert '"${REDIS_PORT:-6379}:6379"' in compose
