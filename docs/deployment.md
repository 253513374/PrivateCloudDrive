# Private Deployment

## Docker Compose

1. Copy `.env.example` to `.env` and change all secrets.
2. Start the backend stack:

```powershell
docker compose up -d --build
```

3. Open Swagger at `http://localhost:8080/swagger`.

The Compose stack contains PostgreSQL, Redis, an API host, a database migrator, a media-worker process for ABP background jobs, and an optional MinIO service behind the `minio` profile.

## Persistent Data

- PostgreSQL data: `privateclouddrive_postgres_data`
- Redis data: `privateclouddrive_redis_data`
- FileCenter blobs, upload temp files, thumbnails, and video covers: `privateclouddrive_storage`
- Optional MinIO data: `privateclouddrive_minio_data`

## Media Processing

The Docker image installs `ffmpeg` and `ffprobe`. The API host disables background job execution, while the `media-worker` service enables it so media thumbnail and metadata jobs are handled separately from HTTP traffic.

## MAUI Client

The seeded mobile OAuth client is `PrivateCloudDrive_App` and uses `privateclouddrive://callback`. Keep `PUBLIC_URL` aligned with the address the mobile device can reach.
