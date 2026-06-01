Cloud Project Backup Utility (Mega Backup Tool)

This console application is a robust solution for automating the backup of working directories containing source code and assets. It is designed for environments requiring regular saving of changes without manual supervision.

Key Features and Workflow:

Incremental Analysis: The application scans specified directories (Freelance, Unity, personal projects) and detects only folders where files have been modified since the last successful run.

Intelligent Filtering: Implements an exclusion system based on blacklists of directory names and full paths (e.g., archive folders and .suo service files are automatically skipped).

Dedicated Unity Support: Features a special archiving mode for Unity engine projects. The utility generates a file list from the Assets folder content and an incremental manifest file, ensuring only relevant resources are packed while excluding the engine's cache and library folders.

Multithreaded Upload: After creating a highly compressed RAR archive (using WinRAR CLI), the file is copied locally and then asynchronously uploaded to Mega.nz cloud storage with real-time progress reporting.

Logging and Idempotency: A marker file (date.txt) stores the timestamp of the last successful backup, preventing file duplication across sessions.

Technology Stack: C#, .NET Core / .NET Framework, MegaApiClient, WinRAR (CLI), File System Management, Asynchronous Programming.
