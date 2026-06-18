from django.db import migrations, models


class Migration(migrations.Migration):

    dependencies = [
        ('accounts', '0004_adminuser_profile_image'),
    ]

    operations = [
        migrations.CreateModel(
            name='AuthTokenRevocation',
            fields=[
                ('id', models.BigAutoField(auto_created=True, primary_key=True, serialize=False, verbose_name='ID')),
                ('token_hash', models.CharField(max_length=64, unique=True)),
                ('jti', models.CharField(blank=True, db_index=True, max_length=255)),
                ('user_identifier', models.CharField(blank=True, max_length=255)),
                ('revoked_at', models.DateTimeField(auto_now_add=True)),
                ('expires_at', models.DateTimeField(blank=True, null=True)),
            ],
        ),
        migrations.AddIndex(
            model_name='authtokenrevocation',
            index=models.Index(fields=['expires_at'], name='accounts_au_expires_474335_idx'),
        ),
    ]
