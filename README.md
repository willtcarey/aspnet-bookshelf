# Bookshelf — A Personal Library Tracker

A personal library application built with ASP.NET MVC. Users can catalog books they own or have read, upload cover images, and write reviews. Built as a learning project to explore ASP.NET MVC and understand how it compares to Ruby on Rails.

The application runs inside Docker for a consistent development environment.

## The Application

Bookshelf lets users:

- Create an account and manage their profile
- Add books to their library with title, author, genre, and description
- Upload cover images for books
- Track reading status (want to read, currently reading, finished)
- Write and edit reviews/notes for books they've read
- Browse and search their collection

## Planned Features

- [x] **Dockerized Development** — Run the entire application in Docker with Docker Compose (`bbc811a`)
- [x] **Database-Backed Models** — Books and Authors with Entity Framework Core (`bbd2690`)
- [x] **Model Editing** — Full CRUD UI for managing books and authors (`bbd2690`)
- [x] **User Accounts** — Registration, login, logout, and authentication/authorization (`f4e6424`)
- [ ] **File Uploading** — Upload and display book cover images
- [x] **Front-End CSS Framework** — Tailwind CSS + DaisyUI integrated with an automatic Docker-based watch/build pipeline (`febec77`)
- [ ] **Admin UI** — Add an admin area with a separate layout for managing site-wide data
- [ ] **Background Job Processing** — Async job queue for tasks like image processing and email delivery

## Models

- **User** — Account info, authentication
- **Book** — Title, description, genre, reading status, cover image
- **Author** — Name, bio (a book belongs to an author)
- **Review** — Rating, text, belongs to a user and a book

## Tech Stack

- **Framework:** ASP.NET MVC (.NET 8)
- **ORM:** Entity Framework Core
- **Database:** PostgreSQL
- **CSS:** Tailwind CSS + DaisyUI
- **Containerization:** Docker & Docker Compose
