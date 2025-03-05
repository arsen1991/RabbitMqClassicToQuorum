# RabbitMqClassicToQuorum

This is a tool to migrate classic queues to quorum without losing a data.

Below are the steps of migration:
1. Create temporary queues with the same bindings.
2. Copy messages from classic queues into temporary queues (is done by publishing message into appropriate exchange(s)).
3. Remove classic queues
4. Create quorum queues.
5. Copy messages from temporary queues.
6. Remove temporary queues (optional).

You may use API or Console App.
