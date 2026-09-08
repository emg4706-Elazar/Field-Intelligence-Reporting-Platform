from confluent_kafka import Producer
import os
import json

# Setup Configuration
input_file = os.getenv(
    "DATA_FILE_PATH",
    "field_reports.json"
)

bootstrap_servers = os.getenv(
    "KAFKA_BOOTSTRAP_SERVERS",
    "localhost:9092"
)

topic_name = os.getenv(
    "KAFKA_TOPIC",
    "messages"
)


# Defined the configuration for producer
config = {"bootstrap.servers": bootstrap_servers}

# Generate the Producer
producer = Producer(config)

with open(input_file, "r", encoding='utf-8') as f:
    dicts = json.load(f)

print("=== Producer Start ====")

for dicti in dicts:
    json_message = json.dumps(dicti)
    producer.produce(
        topic_name,
        key=dicti["reportId"],
        value=json_message
    )

remaining_messages = producer.flush()

if remaining_messages == 0:
    print("All messages were sent successfully.")
else:
    print(f"{remaining_messages} messages were not delivered.")
print("Producer closed.")