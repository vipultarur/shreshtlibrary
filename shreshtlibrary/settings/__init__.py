import os
from .base import *

env = os.environ.get('DJANGO_ENV', 'development')

# Auto-detect Render deployment — force production mode
# Render sets RENDER=true and RENDER_EXTERNAL_HOSTNAME automatically
if os.environ.get('RENDER') == 'true' and env == 'development':
    env = 'production'

if env == 'production':
    from .production import *
elif env == 'staging':
    from .staging import *
else:
    from .development import *
