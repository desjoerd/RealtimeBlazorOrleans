# Realtime blazor and Orleans example apps

This repository contains a set of examples for building realtime applications using Blazor and Orleans.

## 1-blazor

This example shows how to sync data with using a simple singleton. This works with one server, but not with multiple servers.

## 2-blazor-orleans

This example makes the singleton a grain. This allows the application to scale out to multiple servers. The grain is used to store the state of the application, and the client can subscribe to changes in the state.

It contains both an example with Streams and with Grain Observers.

## 3-demo-customer-support

This example shows a more complex application which has a customer support system with chat and having support agents claiming chats.
