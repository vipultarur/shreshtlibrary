import re

with open('script_safe.sql', 'r') as f:
    sql = f.read()

tables = re.findall(r'CREATE TABLE IF NOT EXISTS (\w+) \((.*?)\);', sql, re.DOTALL)

alter_statements = []
for table_name, columns_block in tables:
    lines = columns_block.split('\n')
    for line in lines:
        line = line.strip()
        if not line: continue
        if line.startswith('CONSTRAINT'): continue
        if line.startswith('id '): continue
        if line.startswith('"'): continue
        
        # e.g. ac boolean,
        # or name character varying(100) NOT NULL,
        match = re.match(r'^([a-zA-Z0-9_]+)\s+([^,]+),?', line)
        if match:
            col_name = match.group(1)
            col_def = match.group(2).strip()
            # remove trailing comma if present
            if col_def.endswith(','): col_def = col_def[:-1]
            # don't add column if it's the id or a foreign key reference that shouldn't be added blindly 
            # actually postgres allows ADD COLUMN IF NOT EXISTS, so it's safe to just try to add it.
            alter_stmt = f'ALTER TABLE IF EXISTS {table_name} ADD COLUMN IF NOT EXISTS {col_name} {col_def};'
            alter_statements.append(alter_stmt)

with open('script_safe.sql', 'a') as f:
    f.write('\n\n-- Ensure all columns exist in case tables were previously created by Django\n')
    f.write('\n'.join(alter_statements))
    f.write('\n')
