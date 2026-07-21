# Continuous integration

The CI files in this directory are centrally managed from `MBW.Tool.GithubConfig`. Changes made directly to managed files may be overwritten the next time standard content is applied. Repository-specific behavior is kept in local composite actions so the repository can still describe how it builds and publishes without adding more event-triggered workflows.

The build action may be centrally managed for repositories using a standard build, or maintained by the repository when custom tooling is required. Optional publishing and notification actions are installed only where they are needed.

- `workflows/ci.yml` — orchestrates build, test, packaging, publishing, and release jobs in one workflow run.
- `actions/initialize_ci/action.yml` — resolves the ref, version, and eligible publishing target once.
- `actions/build/action.yml` — builds, tests, and produces artifacts; this file may be centrally managed.
- `actions/collect_publishables/action.yml` — detects packages, Dockerfiles, release assets, and optional integrations.
- `actions/publish_nuget/action.yml` — publishes collected NuGet packages.
- `actions/publish_docker/action.yml` — builds and publishes discovered Docker images.
- `actions/publish_github_release/action.yml` — creates or updates a GitHub Release and uploads its assets.
- `actions/notify_hass_addons/action.yml` — optionally notifies the central Home Assistant add-on repository.
