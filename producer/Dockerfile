FROM python:3.12-slim
WORKDIR /app
COPY requirements.txt .
RUN pip install --no-cache -r requirements.txt
COPY producer.py .
CMD ["python", "producer.py"]