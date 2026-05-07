.PHONY: test test-watch

# Run the .NET test suite inside the web container without spinning up the db
# dependency. Mirrors the invocation we expect CI to use once it's set up.
test:
	docker compose run --rm --no-deps -w /app web dotnet test Bookshelf.sln

test-watch:
	docker compose run --rm --no-deps -w /app web dotnet watch --project Bookshelf.Tests test
