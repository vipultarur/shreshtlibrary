import os
from .base import *

env = os.environ.get('DJANGO_ENV', 'development')

# Render automatically sets RENDER=true. If Django env is unset in Render,
# treat the deployment as production rather than leaving it in development.
if os.environ.get('RENDER') == 'true' and env == 'development':
    env = 'production'

if env == 'production':
    from .production import *
elif env == 'staging':
    from .staging import *
else:
    from .development import *
