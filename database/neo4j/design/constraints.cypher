// Eksisterende Neo4j-design for Rotten Potatoes.
// Eksporteret 2026-11-13 med SHOW CONSTRAINTS fra databasen `rottenpotatoes`
// i containeren neo4jdb (Neo4j 2026.07.1 Enterprise, ~/Projects/neo4j-demo).
// Designet indeholder kun labels og unikke id-constraints: ingen nodes, properties
// ud over id eller relationships er defineret endnu (se docs/database-mapping.md).
//
// Køres IKKE automatisk. Community Edition har kun databasen `neo4j`, så kør dem dér.

CREATE CONSTRAINT `actor_id_unique` FOR (n:`Actor`) REQUIRE (n.`id`) IS UNIQUE;
CREATE CONSTRAINT `award_id_unique` FOR (n:`Award`) REQUIRE (n.`id`) IS UNIQUE;
CREATE CONSTRAINT `director_id_unique` FOR (n:`Director`) REQUIRE (n.`id`) IS UNIQUE;
CREATE CONSTRAINT `genre_id_unique` FOR (n:`Genre`) REQUIRE (n.`id`) IS UNIQUE;
CREATE CONSTRAINT `movie_id_unique` FOR (n:`Movie`) REQUIRE (n.`id`) IS UNIQUE;
CREATE CONSTRAINT `rating_id_unique` FOR (n:`Rating`) REQUIRE (n.`id`) IS UNIQUE;
CREATE CONSTRAINT `review_id_unique` FOR (n:`Review`) REQUIRE (n.`id`) IS UNIQUE;
CREATE CONSTRAINT `series_id_unique` FOR (n:`Series`) REQUIRE (n.`id`) IS UNIQUE;
CREATE CONSTRAINT `streaming_service_id_unique` FOR (n:`StreamingService`) REQUIRE (n.`id`) IS UNIQUE;
CREATE CONSTRAINT `user_activity_id_unique` FOR (n:`UserActivity`) REQUIRE (n.`id`) IS UNIQUE;
CREATE CONSTRAINT `user_id_unique` FOR (n:`User`) REQUIRE (n.`id`) IS UNIQUE;
