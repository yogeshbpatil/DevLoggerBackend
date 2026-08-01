# DOCKER-GRAFANA-SUMMARY

## Overview

This document summarizes the Docker, Prometheus, and Grafana integration
completed for the Developer Journal backend.

## Objectives

-   Install Docker
-   Run Prometheus
-   Run Grafana
-   Expose ASP.NET Core metrics
-   Connect Grafana to Prometheus
-   Build a basic monitoring dashboard

## Architecture

ASP.NET Core API -\> /metrics -\> Prometheus -\> Grafana

## Backend Changes

-   Enabled Prometheus metrics middleware.
-   Exposed `/metrics`.
-   Verified metrics including:
    -   up
    -   http_requests_received_total
    -   http_requests_in_progress
    -   http_request_duration_seconds
    -   process\_\* metrics
    -   dotnet\_\* metrics
    -   EF Core metrics
    -   Npgsql metrics

## Docker Components

-   Docker Desktop
-   Prometheus
-   Grafana

## Grafana Data Source

-   Prometheus
-   URL: http://prometheus:9090

## Dashboard Panels

### API Status

Query:

``` promql
up{job="devlogger-api"}
```

Visualization: Stat Value mappings: - 1 -\> UP - 0 -\> DOWN

### Total Requests

``` promql
sum(http_requests_received_total)
```

### Requests / Second

``` promql
sum(rate(http_requests_received_total[5m]))
```

### Active Requests

``` promql
sum(http_requests_in_progress)
```

### Failed Requests

``` promql
sum(http_requests_received_total{code=~"4..|5.."})
```

## What Was Learned

-   Docker containers
-   Prometheus scraping
-   PromQL basics
-   Grafana panels
-   Stat vs Time Series
-   Value mappings
-   Thresholds
-   Query inspector

## Startup Checklist

1.  Start Docker Desktop.
2.  Start Prometheus.
3.  Start Grafana.
4.  Run backend.
5.  Verify http://localhost:5000/metrics.
6.  Verify Prometheus target is UP.
7.  Open Grafana dashboard.

## Laptop Setup

Repeat Docker installation, run Prometheus and Grafana containers,
configure datasource, run backend, and either recreate or import
dashboards if persistence is not configured.

## Completed

-   Docker integration
-   Prometheus integration
-   Grafana integration
-   Backend metrics
-   Dashboard with:
    -   API Status
    -   Total Requests
    -   Requests/Second
    -   Active Requests
    -   Failed Requests

## Next Steps

-   CPU Usage
-   Memory Usage
-   GC
-   Thread Count
-   Uptime
-   Response Time
-   Database metrics



https://chatgpt.com/c/6a64bc7d-f78c-83ee-b19c-4f9bd084d1fd

http://localhost:5000/metrics

http://localhost:9090/query

http://localhost:9090/targets

http://localhost:3001/?orgId=1&from=now-6h&to=now&timezone=browser