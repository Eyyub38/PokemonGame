import pandas as pd
from datetime import datetime

# Read the excel file
df = pd.read_excel('Assets/Proje.xlsx')

# Generate markdown for planned systems
planned_md = "## Planned / Next Systems\n\n"

# Group by Priority (Önem Sırası)
for priority in df['Önem Sırası'].unique():
    planned_md += f"### {priority}\n\n"
    subset = df[df['Önem Sırası'] == priority]
    for _, row in subset.iterrows():
        name = row['Yapılması Planlananlar']
        area = row['Alanı']
        details = row['Detaylar']
        planned_md += f"- **{name}** ({area}): {details}\n"
    planned_md += "\n"

# Read original GDP parts
with open('Docs/GameDevelopmentPlan.md', 'r', encoding='utf-8') as f:
    content = f.read()

header_part = content.split('### Debug / Validation / Audit')[0].strip()
footer_part = content.split('## Definition of Done')[1].strip()

new_content = f"""{header_part}

{planned_md}## Definition of Done

{footer_part}
"""

with open('Docs/GameDevelopmentPlan.md', 'w', encoding='utf-8') as f:
    f.write(new_content)

print("GameDevelopmentPlan.md updated successfully.")
