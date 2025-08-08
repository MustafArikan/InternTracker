# InternTracker

InternTracker is a comprehensive web application designed to streamline the management and tracking of intern activities, progress, and resources within an organization. It provides distinct roles for Admins, Mentors, and Interns, each with tailored functionalities to facilitate efficient collaboration and oversight.

## Features

### User Management & Authentication
- **Secure User Accounts:** Admins can create and manage user accounts with different roles (Admin, Mentor, Intern).
- **Authentication:** Secure login system with password hashing using BCrypt.NET-Next.
- **Profile Management:** Users can update their email, change passwords, and upload profile pictures.
- **Account Deletion:** Users can delete their accounts, which also removes associated data.

### Intern-Specific Features
- **Task Management:** Interns can view assigned tasks, update their status (Not Started, In Progress, Completed), and log work sessions against tasks.
- **Journaling:** Interns can create, edit, and delete daily journal entries to reflect on their work and learning.
- **Goal Setting:** Interns can set personal goals, track their status (Active, Completed, Reflected), and add reflections.
- **Report Submission:** Interns can submit reports, including file uploads, and receive feedback from mentors.
- **Dashboard:** A personalized dashboard for interns to view their tasks, work sessions, goals, and reports, with visual progress indicators.

### Mentor-Specific Features
- **Intern Oversight:** Mentors can view details of assigned interns, including their tasks, journal entries, goals, reports, and work sessions.
- **Task Assignment:** Mentors can assign new tasks to interns.
- **Feedback:** Mentors can provide feedback on submitted intern reports.
- **Resource Sharing:** Mentors can upload and manage resource files for interns.
- **Progress Dashboard:** Detailed progress dashboard for each intern, showing task status, work session logs, journal entry counts, and report submission trends.

### Admin-Specific Features
- **User Administration:** Admins have full control over user accounts, including creation, editing (e.g., changing roles), and deletion.
- **Resource Management:** Admins can upload, edit, and delete resource files accessible to all users.
- **System Reports:** Generate system-wide reports on user registrations, resource uploads by role, and other key metrics.

### General Features
- **Notifications:** Users receive notifications for relevant events (e.g., new tasks assigned, new resources uploaded, reports submitted).
- **File Uploads:** Secure handling of file uploads for profile pictures, reports, and resource files.
- **Session Management:** Utilizes ASP.NET Core's session management for user state.

## Technologies Used

- **Backend:** ASP.NET Core 9.0 (C#)
- **Database:** SQL Server (via Entity Framework Core)
- **Authentication:** ASP.NET Core Identity with Cookie Authentication, BCrypt.NET-Next for password hashing.
- **Image Processing:** SixLabors.ImageSharp
- **Frontend:** HTML, CSS, JavaScript (Razor Views)
- **Dependency Management:** NuGet, npm (for frontend packages if any, though primarily server-rendered)

## Setup and Installation

1.  **Prerequisites:**
    -   .NET 9.0 SDK
    -   SQL Server (LocalDB or full instance)
    -   Node.js and npm (if frontend dependencies are managed via npm)

2.  **Clone the Repository:**
    ```bash
    git clone https://github.com/MustafArikan/InternTracker.git
    cd InternTracker/InternTracker
    ```

3.  **Database Configuration:**
    -   Open `appsettings.json` (and `appsettings.Development.json`).
    -   Update the `ConnectionStrings:InternTrackerContext` to point to your SQL Server instance.
    -   Example: `"Server=YourServerName;Database=InternTrackerDB;Trusted_Connection=True;MultipleActiveResultSets=true"`

4.  **Run Migrations:**
    -   Open a terminal in the `InternTracker/InternTracker` directory.
    -   Apply database migrations to create the schema:
        ```bash
        dotnet ef database update
        ```

5.  **Run the Application:**
    ```bash
    dotnet run
    ```
    The application will typically run on `https://localhost:7000` (or a similar port).

## How to Use

-   **Registration:** New users can register via the "Get Started" link. By default, new registrations are for the "Intern" role.
-   **Login:** Use the registered credentials to log in.
-   **Role-Based Access:** The application automatically redirects users to their respective dashboards based on their role (Admin, Mentor, Intern).
-   **Admin Panel:** Accessible to users with the "Admin" role for managing users and system resources.
-   **Mentor Panel:** Accessible to users with the "Mentor" role for overseeing interns and assigning tasks.
-   **Intern Panel:** Accessible to users with the "Intern" role for managing their tasks, journals, goals, and reports.

## Contributing

Feel free to fork the repository, open issues, and submit pull requests.
