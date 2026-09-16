FROM python:3.12-slim

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    VISITOR_DATABASE_PATH=/data/visitor_management.sqlite3

RUN useradd --create-home --uid 1000 user \
    && mkdir -p /data \
    && chown user:user /data

USER user
ENV HOME=/home/user
WORKDIR /home/user/app

COPY --chown=user:user app.py index.html ./

EXPOSE 7860

CMD ["python", "app.py", "--host", "0.0.0.0", "--port", "7860", "--no-browser"]