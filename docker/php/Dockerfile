# PHP for the POS project, isolated from the machine's XAMPP.
FROM php:8.4-cli

COPY --from=mlocati/php-extension-installer:2 /usr/bin/install-php-extensions /usr/local/bin/
COPY --from=composer:2 /usr/bin/composer /usr/bin/composer

# intl: Filament | soap: sped-nfe | gd: DANFCE and QR Code | gmp: Laravel 13
# pcntl: queue worker | pdo_mysql: MariaDB | bcmath: money calculations
RUN apt-get update \
 && apt-get install -y --no-install-recommends git unzip tzdata \
 && rm -rf /var/lib/apt/lists/* \
 && install-php-extensions intl soap gd zip bcmath pdo_mysql gmp pcntl opcache

COPY php.ini /usr/local/etc/php/conf.d/zz-pdv.ini

WORKDIR /var/www/html
