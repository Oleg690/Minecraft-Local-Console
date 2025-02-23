import os

def count_lines_in_file(file_path):
    """Counts the number of lines in a given file."""
    try:
        with open(file_path, 'r', encoding='utf-8') as file:
            return sum(1 for _ in file)
    except FileNotFoundError:
        return None

def count_lines_in_files(directory, filenames):
    """Iterates through a list of filenames and counts lines if the file exists in the directory."""
    results = {}
    for filename in filenames:
        file_path = os.path.join(directory, filename)
        line_count = count_lines_in_file(file_path)
        if line_count is not None:
            results[filename] = line_count
        else:
            results[filename] = "File not found"
    return results

directory = "D:\\Minecraft-Server\\important funcs for main aplication\\Create Server Func\\Create Server Func"
file_list = ["dbChanger.cs", "MinecraftServerStats.cs", "serverFileExplorer.cs", "ServerNetworkEnable.cs", "serverOperator.cs", "serverPropriertiesChanger.cs", "versionsUpdater.cs"]
couner = 0

results = count_lines_in_files(directory, file_list)
for file, count in results.items():
    couner += count
    print(f"{file}: {count} lines" if isinstance(count, int) else f"{file}: {count}")
print(f"Total lines: {couner}")