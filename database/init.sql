CREATE TABLE IF NOT EXISTS elements
(
    id BIGSERIAL PRIMARY KEY,
    attribute_value TEXT NOT NULL,
    element_html TEXT NOT NULL
);