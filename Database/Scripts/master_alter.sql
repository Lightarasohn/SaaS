ALTER TABLE app_user ADD COLUMN password_changed_at TIMESTAMP;
ALTER TABLE app_user ALTER COLUMN password_changed_at TYPE TIMESTAMPTZ;
ALTER TABLE user_token ALTER COLUMN token_hash TYPE CHAR(64);
DROP INDEX IF EXISTS IX_UserToken_TokenHash;
CREATE UNIQUE INDEX UX_UserToken_TokenHash ON user_token(token_hash);
CREATE INDEX IX_UserToken_UserId_Type_Active ON user_token(user_id, token_type) WHERE used = FALSE;