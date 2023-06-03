#!/bin/bash
set -e

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
    CREATE ROLE vuta WITH 
        NOSUPERUSER
        NOCREATEDB
        NOCREATEROLE
        NOINHERIT
        LOGIN
        REPLICATION
        CONNECTION LIMIT -1
        PASSWORD '1234';
	CREATE DATABASE vuta;
    ALTER DATABASE vuta OWNER TO vuta;
EOSQL

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname vuta <<-EOSQL
    GRANT ALL ON SCHEMA public TO vuta;
	GRANT ALL PRIVILEGES ON DATABASE vuta TO vuta;
EOSQL

PGPASSWORD=1234 psql -v ON_ERROR_STOP=1 --username vuta --dbname vuta <<-EOSQL
    CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
        migration_id character varying(150) NOT NULL,
        product_version character varying(32) NOT NULL,
        CONSTRAINT pk___ef_migrations_history PRIMARY KEY (migration_id)
    );

    START TRANSACTION;

    CREATE TABLE channels (
        id text NOT NULL,
        title text NOT NULL,
        thumbnail text NOT NULL,
        CONSTRAINT pk_channels PRIMARY KEY (id)
    );

    CREATE TABLE videos (
        id text NOT NULL,
        channel_id text NOT NULL,
        title text NOT NULL,
        publish_date timestamp with time zone NOT NULL,
        CONSTRAINT pk_videos PRIMARY KEY (id),
        CONSTRAINT fk_videos_channels_channel_id FOREIGN KEY (channel_id) REFERENCES channels (id) ON DELETE CASCADE
    );

    CREATE TABLE comments (
        id text NOT NULL,
        video_id text NOT NULL,
        text text NOT NULL,
        like_count bigint NOT NULL,
        replies_id text NULL,
        CONSTRAINT pk_comments PRIMARY KEY (id),
        CONSTRAINT fk_comments_videos_video_id FOREIGN KEY (video_id) REFERENCES videos (id) ON DELETE CASCADE
    );

    CREATE INDEX ix_comments_video_id ON comments (video_id);

    CREATE INDEX ix_videos_channel_id ON videos (channel_id);

    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20230423135546_initial', '7.0.5');

    COMMIT;

    START TRANSACTION;

    ALTER TABLE videos ADD last_comment_scan timestamp with time zone NULL;

    ALTER TABLE videos ADD last_update timestamp with time zone NULL;

    ALTER TABLE videos ADD next_comment_scan timestamp with time zone NULL;

    ALTER TABLE videos ADD next_update timestamp with time zone NULL;

    ALTER TABLE channels ADD last_update timestamp with time zone NULL;

    ALTER TABLE channels ADD last_video_scan timestamp with time zone NULL;

    ALTER TABLE channels ADD next_update timestamp with time zone NULL;

    ALTER TABLE channels ADD next_video_scan timestamp with time zone NULL;

    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20230425051934_scan_tracking', '7.0.5');

    COMMIT;

    START TRANSACTION;

    CREATE INDEX ix_videos_last_comment_scan ON videos (last_comment_scan);

    CREATE INDEX ix_videos_last_update ON videos (last_update);

    CREATE INDEX ix_videos_next_comment_scan ON videos (next_comment_scan);

    CREATE INDEX ix_videos_next_update ON videos (next_update);

    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20230425062715_addVideoIndexes', '7.0.5');

    COMMIT;

    START TRANSACTION;

    ALTER TABLE videos ADD next_update_id uuid NULL;

    CREATE INDEX ix_videos_next_update_id ON videos (next_update_id);

    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20230425071249_addNextUpdateId', '7.0.5');

    COMMIT;

    START TRANSACTION;

    ALTER TABLE videos ADD is_uta boolean NOT NULL DEFAULT FALSE;

    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20230425084106_addIsUta', '7.0.5');

    COMMIT;

    START TRANSACTION;

    ALTER TABLE channels ADD next_update_id uuid NULL;

    CREATE INDEX ix_channels_last_update ON channels (last_update);

    CREATE INDEX ix_channels_last_video_scan ON channels (last_video_scan);

    CREATE INDEX ix_channels_next_update ON channels (next_update);

    CREATE INDEX ix_channels_next_update_id ON channels (next_update_id);

    CREATE INDEX ix_channels_next_video_scan ON channels (next_video_scan);

    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20230426103642_addChannelNextUpdateId_Index', '7.0.5');

    COMMIT;

    START TRANSACTION;

    DROP INDEX ix_videos_channel_id;

    DROP INDEX ix_videos_last_comment_scan;

    DROP INDEX ix_videos_last_update;

    DROP INDEX ix_videos_next_comment_scan;

    DROP INDEX ix_videos_next_update;

    DROP INDEX ix_videos_next_update_id;

    CREATE INDEX ix_videos_channel_id_publish_date ON videos (channel_id, publish_date);

    CREATE INDEX ix_videos_next_update_next_update_id ON videos (next_update, next_update_id);

    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20230426143312_reduceIndex', '7.0.5');

    COMMIT;

    START TRANSACTION;

    ALTER TABLE comments ADD last_update timestamp with time zone NOT NULL DEFAULT TIMESTAMPTZ '-infinity';

    CREATE INDEX ix_comments_last_update ON comments (last_update);

    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20230426154704_commentLastUpdate', '7.0.5');

    COMMIT;

    START TRANSACTION;

    ALTER TABLE channels ADD handle text NULL;

    CREATE INDEX ix_channels_handle ON channels (handle);

    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20230503052237_addChannelHandle', '7.0.5');

    COMMIT;

    START TRANSACTION;

    ALTER TABLE videos ADD unavailable_since timestamp with time zone NULL;

    ALTER TABLE channels ADD unavailable_since timestamp with time zone NULL;

    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20230503061210_addUnavailableSince', '7.0.5');

    COMMIT;
    
	START TRANSACTION;

	ALTER TABLE channels ADD banner text NULL;

	ALTER TABLE channels ADD description text NOT NULL DEFAULT '';

	ALTER TABLE channels ADD subscriber_count bigint NULL;

	ALTER TABLE channels ADD video_count bigint NOT NULL DEFAULT 0;

	INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
	VALUES ('20230603085220_extendChannelInfo', '7.0.5');

	COMMIT;

    CREATE PUBLICATION es_indexer_pub FOR TABLE videos, channels, comments;
    SELECT * FROM pg_create_logical_replication_slot('es_indexer_slot', 'pgoutput');
EOSQL